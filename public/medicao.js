// Medicao de audiencia do CronoAula, no modelo do Escalada.dev.
//
// A tag do Google nao fica no HTML. Ao abrir a pagina este script le a escolha
// guardada: "aceito" injeta a tag na hora, "recusado" nao faz nada, e sem
// escolha mostra o aviso e espera. Antes do aceite nenhuma requisicao vai ao
// Google.
//
// A escolha fica numa chave so do localStorage, sem prazo. Nao e cookie e nao
// vai para o servidor. Como o localStorage e separado por subdominio, cada
// produto do Escalada pergunta uma vez.

const MEDIDOR = 'G-FGK4P7BHHK';
const CHAVE = 'cronoaula.medicao';
const POLITICA = new URL('privacidade.html', import.meta.url);

const guardado = () => {
  try {
    return localStorage.getItem(CHAVE);
  } catch {
    // Navegador anonimo ou armazenamento bloqueado: trata como sem escolha.
    return null;
  }
};

const guardar = (valor) => {
  try {
    localStorage.setItem(CHAVE, valor);
  } catch {
    // Sem onde guardar, a escolha vale so para esta visita.
  }
};

const esquecer = () => {
  try {
    localStorage.removeItem(CHAVE);
  } catch {
    // Nada a fazer.
  }
};

/** Endereco sem parametro nem ancora, para nada extra viajar junto. */
function caminhoLimpo(endereco) {
  try {
    const url = new URL(endereco);
    return url.origin + url.pathname;
  } catch {
    return '';
  }
}

function ligarGoogle() {
  window.dataLayer = window.dataLayer || [];
  function gtag() {
    window.dataLayer.push(arguments);
  }
  window.gtag = gtag;

  gtag('js', new Date());
  gtag('config', MEDIDOR, {
    // O Google recebe o caminho da pagina, e nao o endereco inteiro. Se um dia
    // alguma tela usar parametro, ele nao vai junto.
    page_location: caminhoLimpo(location.href),
    page_referrer: caminhoLimpo(document.referrer),
    anonymize_ip: true,
    allow_google_signals: false,
    allow_ad_personalization_signals: false,
  });

  const tag = document.createElement('script');
  tag.async = true;
  tag.src = 'https://www.googletagmanager.com/gtag/js?id=' + MEDIDOR;
  document.head.appendChild(tag);
}

function desligarGoogle() {
  // Chave de desligamento do proprio Google, para o caso de a pessoa mudar de
  // ideia depois de ter aceitado nesta mesma visita.
  window['ga-disable-' + MEDIDOR] = true;
}

// ---------------------------------------------------------------- o aviso

function montarAviso() {
  const aviso = document.createElement('section');
  aviso.className = 'medicao-aviso';
  aviso.setAttribute('role', 'dialog');
  aviso.setAttribute('aria-labelledby', 'medicao-titulo');

  const texto = document.createElement('div');
  texto.className = 'medicao-texto';

  const titulo = document.createElement('h2');
  titulo.id = 'medicao-titulo';
  titulo.textContent = 'Medição de audiência';

  // O aviso e o momento em que a pessoa decide. Por isso ele diz, aqui mesmo,
  // que os dados saem do Brasil: a LGPD, no art. 33, VIII, pede informacao
  // previa sobre o carater internacional da transferencia no proprio pedido.
  const frase = document.createElement('p');
  frase.append(
    'Este site pode medir quantas pessoas abrem cada página, usando o Google Analytics. '
    + 'Serve para saber o que é usado e o que não é. Os dados vão para a Google LLC, '
    + 'nos Estados Unidos, fora do Brasil. Se você recusar, nada é enviado e o site funciona igual. '
  );

  const comoFunciona = document.createElement('a');
  comoFunciona.href = POLITICA.href;
  comoFunciona.textContent = 'Como isso funciona';
  frase.append(comoFunciona, '.');

  texto.append(titulo, frase);

  // Os dois botoes tem o mesmo tamanho, peso, cor e posicao: recusar nao pode
  // ser mais dificil do que aceitar.
  const botoes = document.createElement('div');
  botoes.className = 'medicao-botoes';

  const aceitar = document.createElement('button');
  aceitar.type = 'button';
  aceitar.className = 'medicao-botao';
  aceitar.textContent = 'Aceitar';
  aceitar.addEventListener('click', () => {
    guardar('aceito');
    aviso.remove();
    ligarGoogle();
  });

  const recusar = document.createElement('button');
  recusar.type = 'button';
  recusar.className = 'medicao-botao';
  recusar.textContent = 'Recusar';
  recusar.addEventListener('click', () => {
    guardar('recusado');
    aviso.remove();
    desligarGoogle();
  });

  botoes.append(aceitar, recusar);
  aviso.append(texto, botoes);
  return aviso;
}

function mostrarAviso() {
  if (document.querySelector('.medicao-aviso')) return;
  // Entra no comeco do corpo da pagina, para os dois botoes serem os primeiros
  // na ordem do teclado.
  document.body.prepend(montarAviso());
}

// ------------------------------------------------------------ rever a escolha

// O link fica no rodape de todas as paginas e na politica. Ele apaga a escolha
// e mostra o aviso de novo, na mesma pagina.
document.addEventListener('click', (e) => {
  const link = e.target.closest('.rever-medicao');
  if (!link) return;
  e.preventDefault();
  esquecer();
  desligarGoogle();
  mostrarAviso();
});

// ---------------------------------------------------------------- partida

const escolha = guardado();
if (escolha === 'aceito') ligarGoogle();
else if (escolha === 'recusado') desligarGoogle();
else mostrarAviso();

// Contagem regressiva. Mesmas regras do TimerEngine do programa de Windows.
//
// O restante e sempre (duracao - tempo decorrido), com o decorrido lido de um
// relogio monotonico. Nunca se soma tick de temporizador, entao milhares de
// atualizacoes nao acumulam erro.

export function criarMotor({ relogio, negativo = true, avisoEm = null }) {
  let duracaoMs = 0;
  let acumulado = 0;   // decorrido ate a ultima pausa
  let inicio = 0;      // leitura do relogio quando a contagem atual comecou
  let estado = 'parado';
  let fimDisparado = false;
  let avisoDisparado = false;
  let contagemNegativa = negativo;
  let limiarAviso = avisoEm;

  const decorrido = () => (estado === 'contando' ? acumulado + (relogio() - inicio) : acumulado);
  const bruto = () => duracaoMs - decorrido();

  function carregar(ms) {
    duracaoMs = Math.max(0, ms);
    acumulado = 0;
    fimDisparado = false;
    avisoDisparado = false;
    estado = 'parado';
  }

  function iniciar() {
    if (estado === 'contando') return;
    inicio = relogio();
    estado = 'contando';
  }

  function pausar() {
    if (estado !== 'contando') return;
    acumulado += relogio() - inicio;
    estado = 'pausado';
  }

  function zerar() {
    acumulado = 0;
    fimDisparado = false;
    avisoDisparado = false;
    estado = 'parado';
  }

  function somar(ms) {
    duracaoMs = Math.max(0, duracaoMs + ms);
    // Voltou ao positivo: o fim pode acontecer de novo mais tarde.
    if (bruto() > 0) fimDisparado = false;
  }

  function atualizar() {
    if (estado !== 'contando') return [];
    const eventos = [];
    const rem = bruto();

    if (limiarAviso !== null && !avisoDisparado && rem <= limiarAviso && rem > 0) {
      avisoDisparado = true;
      eventos.push('aviso');
    }

    if (rem <= 0 && !fimDisparado) {
      fimDisparado = true;
      eventos.push('fim');
      if (!contagemNegativa) {
        acumulado = decorrido();
        estado = 'parado';
      }
    }

    return eventos;
  }

  function restante() {
    const rem = bruto();
    return !contagemNegativa && rem < 0 ? 0 : rem;
  }

  function configurar(opcoes) {
    if ('negativo' in opcoes) contagemNegativa = opcoes.negativo;
    if ('avisoEm' in opcoes) limiarAviso = opcoes.avisoEm;
  }

  return {
    carregar,
    iniciar,
    pausar,
    zerar,
    somar,
    atualizar,
    restante,
    duracao: () => duracaoMs,
    estado: () => estado,
    estourado: () => restante() < 0,
    configurar,
  };
}

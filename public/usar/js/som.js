// Os dois sons do CronoAula, agendados no relogio do audio do navegador.
//
// Com a aba em segundo plano, o Chrome atrasa temporizadores em ate um minuto.
// O relogio do audio nao sofre esse atraso, entao o som e agendado com
// AudioBufferSourceNode.start(quando) em vez de disparado por setTimeout.

const MAX_TOQUES = 60;
const VOLUME_AVISO = 0.7;

const SONS = {
  alerta: new URL('../sons/alerta.wav', import.meta.url),
  aviso: new URL('../sons/aviso.wav', import.meta.url),
};

/** O que tocar e quando, em segundos a partir de agora. So entra o que esta no futuro. */
export function planejar({ fimEmMs, avisoEmMs, repeticoes, intervaloS, volume, ligado }) {
  const plano = [];
  if (!ligado) return plano;

  if (avisoEmMs !== null && avisoEmMs !== undefined && avisoEmMs > 0)
    plano.push({ som: 'aviso', emS: avisoEmMs / 1000, volume: volume * VOLUME_AVISO });

  if (fimEmMs >= 0) {
    const toques = repeticoes > 0 ? Math.min(repeticoes, MAX_TOQUES) : MAX_TOQUES;
    for (let i = 0; i < toques; i++)
      plano.push({ som: 'alerta', emS: fimEmMs / 1000 + i * intervaloS, volume });
  }

  return plano;
}

export function criarSom({
  criarContexto = () => {
    // Safari 16.4+ no iPhone: sem isto, a chave lateral de silencio muda o som para mudo
    if (navigator.audioSession) {
      try {
        navigator.audioSession.type = 'playback';
      } catch {
        // navegador sem suporte a este campo: segue sem
      }
    }
    return new (window.AudioContext || window.webkitAudioContext)();
  },
  buscar = (url) => fetch(url),
} = {}) {
  let ctx = null;
  let buffers = null;
  let carregando = null;
  let agendados = [];  // { fonte, tipo: 'alerta' | 'aviso' | 'teste', quando }
  let geracao = 0;     // invalida um agendar que ainda esperava os sons carregarem
  let pendente = null; // { opcoes, base, geracao } do ultimo agendar que ficou sem sons

  async function carregarBuffers() {
    try {
      const [alerta, aviso] = await Promise.all([SONS.alerta, SONS.aviso].map(async (url) => {
        const resposta = await buscar(url);
        if (!resposta.ok) throw new Error('som indisponivel');
        return ctx.decodeAudioData(await resposta.arrayBuffer());
      }));
      buffers = { alerta, aviso };
      // um gesto anterior tentou agendar sem sucesso; agora que os sons chegaram, agenda do jeito que ficou pendente
      if (pendente && pendente.geracao === geracao) {
        const { opcoes, base } = pendente;
        pendente = null;
        for (const item of planejar(opcoes)) tocar(item.som, base + item.emS, item.volume, item.som);
      }
    } catch {
      carregando = null; // tenta de novo no proximo gesto
    }
  }

  /** Chamado a cada clique ou tecla: o navegador so libera audio depois de um gesto. */
  function liberar() {
    if (!ctx) {
      try {
        ctx = criarContexto();
      } catch {
        return Promise.resolve();
      }
    }
    if (ctx.state !== 'running') ctx.resume().catch(() => {});
    if (!buffers && !carregando) carregando = carregarBuffers();
    return carregando ?? Promise.resolve();
  }

  function parar(fonte) {
    try {
      fonte.stop();
    } catch {
      // ja tinha parado
    }
  }

  function tocar(chave, quando, volume, tipo) {
    const fonte = ctx.createBufferSource();
    fonte.buffer = buffers[chave];
    const ganho = ctx.createGain();
    ganho.gain.value = volume;
    fonte.connect(ganho);
    ganho.connect(ctx.destination);
    fonte.start(Math.max(quando, ctx.currentTime));
    agendados.push({ fonte, tipo, quando });
  }

  function cancelar() {
    geracao++;
    pendente = null;
    for (const a of agendados) parar(a.fonte);
    agendados = [];
  }

  async function agendar(opcoes) {
    cancelar();
    const minha = geracao;
    if (!ctx) return;
    const base = ctx.currentTime;
    // guarda a intencao deste agendar: se os sons ainda nao chegaram, carregarBuffers usa isto depois
    pendente = { opcoes, base, geracao: minha };
    await liberar();
    if (minha !== geracao) return;
    if (!buffers) return; // sons ainda nao chegaram; fica pendente para quando chegarem
    pendente = null;
    for (const item of planejar(opcoes)) tocar(item.som, base + item.emS, item.volume, item.som);
  }

  /** Interrompe o alerta de fim se ele ja comecou. Alerta ainda no futuro continua agendado. */
  function silenciar() {
    if (!ctx) return false;
    const alertas = agendados.filter((a) => a.tipo === 'alerta');
    if (!alertas.some((a) => a.quando <= ctx.currentTime)) return false;
    for (const a of alertas) parar(a.fonte);
    agendados = agendados.filter((a) => a.tipo !== 'alerta');
    return true;
  }

  async function testar(chave, volume) {
    await liberar();
    if (!buffers) return;
    tocar(chave, ctx.currentTime, volume, 'teste');
  }

  return {
    liberar,
    agendar,
    cancelar,
    silenciar,
    testarAlerta: (volume) => testar('alerta', volume),
    testarAviso: (volume) => testar('aviso', volume * VOLUME_AVISO),
  };
}

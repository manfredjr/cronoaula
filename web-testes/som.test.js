import { test } from 'node:test';
import assert from 'node:assert/strict';
import { planejar, criarSom } from '../public/usar/js/som.js';

const base = { fimEmMs: 10000, avisoEmMs: 4000, repeticoes: 5, intervaloS: 3, volume: 0.5, ligado: true };

test('plano: aviso uma vez a 70% e alerta repetido a partir do zero', () => {
  assert.deepEqual(planejar(base), [
    { som: 'aviso', emS: 4, volume: 0.35 },
    { som: 'alerta', emS: 10, volume: 0.5 },
    { som: 'alerta', emS: 13, volume: 0.5 },
    { som: 'alerta', emS: 16, volume: 0.5 },
    { som: 'alerta', emS: 19, volume: 0.5 },
    { som: 'alerta', emS: 22, volume: 0.5 },
  ]);
});

test('plano: zero repetições toca 60 vezes, e o teto é 60', () => {
  assert.equal(planejar({ ...base, avisoEmMs: null, repeticoes: 0 }).length, 60);
  assert.equal(planejar({ ...base, avisoEmMs: null, repeticoes: 100 }).length, 60);
});

test('plano: som desligado não toca nada', () => {
  assert.deepEqual(planejar({ ...base, ligado: false }), []);
});

test('plano: só o que está no futuro', () => {
  assert.deepEqual(planejar({ ...base, avisoEmMs: null }).filter((p) => p.som === 'aviso'), []);
  assert.deepEqual(planejar({ ...base, avisoEmMs: -1 }).filter((p) => p.som === 'aviso'), []);
  // fimEmMs 0 e o fim chegando agora: o alerta ainda toca, a partir de emS 0.
  assert.deepEqual(planejar({ ...base, avisoEmMs: null, fimEmMs: 0 }), [
    { som: 'alerta', emS: 0, volume: 0.5 },
    { som: 'alerta', emS: 3, volume: 0.5 },
    { som: 'alerta', emS: 6, volume: 0.5 },
    { som: 'alerta', emS: 9, volume: 0.5 },
    { som: 'alerta', emS: 12, volume: 0.5 },
  ]);
  assert.deepEqual(planejar({ ...base, avisoEmMs: null, fimEmMs: -5000 }), []);
});

// Contexto de audio falso: registra o que foi agendado.
function criarCtxFalso(fontes) {
  return {
    currentTime: 0,
    state: 'running',
    destination: {},
    resume() { this.state = 'running'; return Promise.resolve(); },
    decodeAudioData: (dados) => Promise.resolve({ nome: dados.nome }),
    createGain: () => ({ gain: { value: 1 }, connect() {} }),
    createBufferSource() {
      const fonte = {
        buffer: null, ganho: null, inicio: null, parado: false,
        connect(g) { this.ganho = g; },
        start(quando) { this.inicio = quando; },
        stop() { this.parado = true; },
      };
      fontes.push(fonte);
      return fonte;
    },
  };
}

function montar() {
  const fontes = [];
  const ctx = criarCtxFalso(fontes);
  const buscar = async (url) => ({
    ok: true,
    arrayBuffer: async () => ({ nome: String(url).includes('aviso') ? 'aviso' : 'alerta' }),
  });
  const som = criarSom({ criarContexto: () => ctx, buscar });
  return { ctx, fontes, som };
}

// As duas primeiras chamadas de buscar (a primeira rodada de carregarBuffers) falham;
// da terceira em diante (segunda rodada) tem sucesso.
function montarComBuscarFalho() {
  const fontes = [];
  const ctx = criarCtxFalso(fontes);
  let chamadas = 0;
  const buscar = async (url) => {
    chamadas++;
    if (chamadas <= 2) throw new Error('falha de rede');
    return {
      ok: true,
      arrayBuffer: async () => ({ nome: String(url).includes('aviso') ? 'aviso' : 'alerta' }),
    };
  };
  const som = criarSom({ criarContexto: () => ctx, buscar });
  return { ctx, fontes, som };
}

test('agendar antes de liberar não quebra e não agenda', async () => {
  const { fontes, som } = montar();
  await som.agendar(base);
  assert.equal(fontes.length, 0);
});

test('agendar põe cada som no relógio do áudio, com o volume certo', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  assert.deepEqual(fontes.map((f) => [f.buffer.nome, f.inicio, f.ganho.gain.value]), [
    ['aviso', 4, 0.35],
    ['alerta', 10, 0.5],
    ['alerta', 13, 0.5],
    ['alerta', 16, 0.5],
    ['alerta', 19, 0.5],
    ['alerta', 22, 0.5],
  ]);
});

test('agendar de novo cancela o anterior', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  await som.agendar({ ...base, avisoEmMs: null, repeticoes: 1 });
  assert.equal(fontes.slice(0, 6).every((f) => f.parado), true);
  assert.equal(fontes.length, 7);
  assert.equal(fontes[6].parado, false);
});

test('silenciar não cancela um alerta que ainda não começou', async () => {
  const { ctx, fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  ctx.currentTime = 9;
  assert.equal(som.silenciar(), false);
  assert.equal(fontes.some((f) => f.parado), false);
});

test('silenciar interrompe o alerta que já começou e as repetições restantes', async () => {
  const { ctx, fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  ctx.currentTime = 11;
  assert.equal(som.silenciar(), true);
  assert.equal(fontes.filter((f) => f.buffer.nome === 'alerta').every((f) => f.parado), true);
  assert.equal(som.silenciar(), false);
});

test('um teste de som tocando não faz o silenciar cancelar o alerta futuro', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  await som.testarAlerta(0.5);
  assert.equal(som.silenciar(), false);
  assert.equal(fontes.slice(0, 6).some((f) => f.parado), false);
});

test('testar toca na hora; o aviso a 70%', async () => {
  const { ctx, fontes, som } = montar();
  ctx.currentTime = 3;
  await som.testarAviso(1);
  assert.equal(fontes.length, 1);
  assert.equal(fontes[0].buffer.nome, 'aviso');
  assert.equal(fontes[0].inicio, 3);
  assert.equal(fontes[0].ganho.gain.value, 0.7);
});

test('cancelar para tudo', async () => {
  const { fontes, som } = montar();
  await som.liberar();
  await som.agendar(base);
  som.cancelar();
  assert.equal(fontes.every((f) => f.parado), true);
});

test('som que falhou ao carregar e agendado quando um gesto seguinte consegue carregar', async () => {
  const { fontes, som } = montarComBuscarFalho();
  som.liberar(); // gesto que dispara a carga (sem esperar), como o pointerdown de verdade
  await som.agendar(base); // o agendar entra na mesma rodada, que falha: nao agenda nada
  assert.equal(fontes.length, 0);
  await som.liberar(); // segundo gesto: carrega com sucesso e agenda o que ficou pendente
  assert.deepEqual(fontes.map((f) => [f.buffer.nome, f.inicio, f.ganho.gain.value]), [
    ['aviso', 4, 0.35],
    ['alerta', 10, 0.5],
    ['alerta', 13, 0.5],
    ['alerta', 16, 0.5],
    ['alerta', 19, 0.5],
    ['alerta', 22, 0.5],
  ]);
});

test('cancelar depois de uma carga falha nao deixa nada agendado quando os sons chegam depois', async () => {
  const { fontes, som } = montarComBuscarFalho();
  som.liberar();
  await som.agendar(base);
  som.cancelar();
  await som.liberar();
  assert.equal(fontes.length, 0);
});

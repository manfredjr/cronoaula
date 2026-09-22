import { test } from 'node:test';
import assert from 'node:assert/strict';
import { CHAVE, PADROES, normalizar, carregar, salvar } from '../public/usar/js/ajustes.js';

function armazenamentoFalso(inicial = {}) {
  const dados = new Map(Object.entries(inicial));
  return {
    dados,
    getItem: (k) => (dados.has(k) ? dados.get(k) : null),
    setItem: (k, v) => { dados.set(k, String(v)); },
  };
}

const quebrado = {
  getItem() { throw new Error('bloqueado'); },
  setItem() { throw new Error('bloqueado'); },
};

test('sem nada guardado, usa os padrões', () => {
  assert.deepEqual(carregar(armazenamentoFalso()), PADROES);
});

test('os padrões são os da especificação', () => {
  assert.deepEqual({ ...PADROES }, {
    som: true, volume: 0.7, repeticoes: 5, intervalo: 3,
    avisoLigado: true, avisoMinutos: 5, negativo: true, ultimoMinutos: 50,
  });
});

test('salvar e carregar devolve o mesmo', () => {
  const a = armazenamentoFalso();
  const meus = { ...PADROES, som: false, volume: 0.3, repeticoes: 0, intervalo: 10, avisoMinutos: 2, negativo: false, ultimoMinutos: 7.5 };
  assert.equal(salvar(a, meus), true);
  assert.ok(a.dados.has(CHAVE));
  assert.deepEqual(carregar(a), meus);
});

test('valores fora da faixa são trazidos para dentro', () => {
  const r = normalizar({ volume: 3, repeticoes: 999, intervalo: 0, avisoMinutos: 500 });
  assert.equal(r.volume, 1);
  assert.equal(r.repeticoes, 60);
  assert.equal(r.intervalo, 1);
  assert.equal(r.avisoMinutos, 120);
  const s = normalizar({ volume: -1, repeticoes: -3, intervalo: 99, avisoMinutos: 0 });
  assert.equal(s.volume, 0);
  assert.equal(s.repeticoes, 0);
  assert.equal(s.intervalo, 30);
  assert.equal(s.avisoMinutos, 1);
});

test('contagens viram números inteiros', () => {
  const r = normalizar({ repeticoes: 2.6, intervalo: 4.2, avisoMinutos: 3.5 });
  assert.equal(r.repeticoes, 3);
  assert.equal(r.intervalo, 4);
  assert.equal(r.avisoMinutos, 4);
});

test('último tempo fora de 0 a 1440 volta para 50', () => {
  assert.equal(normalizar({ ultimoMinutos: 2000 }).ultimoMinutos, 50);
  assert.equal(normalizar({ ultimoMinutos: -1 }).ultimoMinutos, 50);
  assert.equal(normalizar({ ultimoMinutos: 0 }).ultimoMinutos, 0);
  assert.equal(normalizar({ ultimoMinutos: 1440 }).ultimoMinutos, 1440);
});

test('tipo errado volta ao padrão', () => {
  const r = normalizar({ som: 'sim', volume: '0.5', repeticoes: null, negativo: 1, ultimoMinutos: 'x' });
  assert.equal(r.som, true);
  assert.equal(r.volume, 0.7);
  assert.equal(r.repeticoes, 5);
  assert.equal(r.negativo, true);
  assert.equal(r.ultimoMinutos, 50);
});

test('JSON corrompido volta aos padrões', () => {
  assert.deepEqual(carregar(armazenamentoFalso({ [CHAVE]: '{isso não é json' })), PADROES);
  assert.deepEqual(carregar(armazenamentoFalso({ [CHAVE]: '42' })), PADROES);
  assert.deepEqual(carregar(armazenamentoFalso({ [CHAVE]: 'null' })), PADROES);
});

test('armazenamento que lança erro não impede de funcionar', () => {
  assert.deepEqual(carregar(quebrado), PADROES);
  assert.equal(salvar(quebrado, PADROES), false);
});

test('sem armazenamento nenhum, também funciona', () => {
  assert.deepEqual(carregar(null), PADROES);
  assert.equal(salvar(null, PADROES), false);
});

test('carregar devolve cópia, sem expor os padrões', () => {
  const r = carregar(armazenamentoFalso());
  r.volume = 0.1;
  assert.equal(PADROES.volume, 0.7);
});

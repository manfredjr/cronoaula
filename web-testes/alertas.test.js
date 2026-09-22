import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { faixa, nome, estadoEscrito } from '../public/usar/js/alertas.js';

const casos = JSON.parse(readFileSync(new URL('../casos-compartilhados.json', import.meta.url), 'utf8'));

test('faixa segue os casos compartilhados', () => {
  for (const c of casos.faixa)
    assert.equal(faixa(c.restanteMs, c.duracaoMs), c.faixa, `${c.restanteMs} de ${c.duracaoMs}`);
});

test('cada faixa não normal tem nome escrito', () => {
  assert.equal(nome('normal'), '');
  assert.equal(nome('atencao'), 'reta final');
  assert.equal(nome('urgente'), 'último minuto');
  assert.equal(nome('estourado'), 'tempo excedido');
});

test('linha de estado: pausado vem primeiro', () => {
  assert.equal(estadoEscrito({ estado: 'pausado', faixa: 'estourado', armado: null, toque: false }), 'pausado');
});

test('linha de estado: tempo excedido vem antes do tempo carregado', () => {
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'estourado', armado: 5, toque: false }), 'tempo excedido');
});

test('linha de estado: tempo rápido carregado, com clique ou toque', () => {
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'normal', armado: 10, toque: false }),
    '10 min carregado. Clique de novo para iniciar');
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'normal', armado: 10, toque: true }),
    '10 min carregado. Toque de novo para iniciar');
});

test('linha de estado: tempo carregado não aparece contando', () => {
  assert.equal(estadoEscrito({ estado: 'contando', faixa: 'normal', armado: 10, toque: false }), '');
});

test('linha de estado: nome da faixa, contando ou parado', () => {
  assert.equal(estadoEscrito({ estado: 'contando', faixa: 'atencao', armado: null, toque: false }), 'reta final');
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'urgente', armado: null, toque: false }), 'último minuto');
});

test('linha de estado: vazia contando no normal, pronto parado', () => {
  assert.equal(estadoEscrito({ estado: 'contando', faixa: 'normal', armado: null, toque: false }), '');
  assert.equal(estadoEscrito({ estado: 'parado', faixa: 'normal', armado: null, toque: false }), 'pronto');
});

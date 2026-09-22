import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { interpretar, formatar } from '../public/usar/js/tempo.js';

const casos = JSON.parse(readFileSync(new URL('../casos-compartilhados.json', import.meta.url), 'utf8'));

test('interpretar segue os casos compartilhados', () => {
  for (const { texto, ms } of casos.interpretar)
    assert.equal(interpretar(texto), ms, `"${texto}"`);
});

test('formatar segue os casos compartilhados', () => {
  for (const { ms, texto } of casos.formatar)
    assert.equal(formatar(ms), texto, `${ms} ms`);
});

test('interpretar rejeita o que não é texto', () => {
  assert.equal(interpretar(null), null);
  assert.equal(interpretar(undefined), null);
  assert.equal(interpretar(25), null);
});

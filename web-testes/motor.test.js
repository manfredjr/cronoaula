import { test } from 'node:test';
import assert from 'node:assert/strict';
import { criarMotor } from '../public/usar/js/motor.js';

const MINUTO = 60000;
const SEGUNDO = 1000;

// Relogio falso: so anda quando o teste manda.
function relogioFalso() {
  let agora = 0;
  const relogio = () => agora;
  relogio.avancar = (ms) => { agora += ms; };
  return relogio;
}

function montar(opcoes = {}) {
  const relogio = relogioFalso();
  const motor = criarMotor({ relogio, ...opcoes });
  return { relogio, motor };
}

test('carregar deixa pronto com o tempo cheio, sem contar', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  assert.equal(motor.estado(), 'parado');
  assert.equal(motor.restante(), 10 * MINUTO);
  relogio.avancar(5 * MINUTO);
  assert.equal(motor.restante(), 10 * MINUTO);
});

test('carregar tempo negativo vira zero', () => {
  const { motor } = montar();
  motor.carregar(-5 * SEGUNDO);
  assert.equal(motor.duracao(), 0);
});

test('iniciar conta regressivo', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(3 * MINUTO);
  assert.equal(motor.estado(), 'contando');
  assert.equal(motor.restante(), 7 * MINUTO);
});

test('iniciar com o cronômetro já contando não faz nada', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(2 * MINUTO);
  motor.iniciar();
  relogio.avancar(1 * MINUTO);
  assert.equal(motor.restante(), 7 * MINUTO);
});

test('pausar congela o tempo e continuar retoma', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(2 * MINUTO);
  motor.pausar();
  relogio.avancar(5 * MINUTO);
  assert.equal(motor.restante(), 8 * MINUTO);
  assert.equal(motor.estado(), 'pausado');
  motor.iniciar();
  relogio.avancar(3 * MINUTO);
  assert.equal(motor.restante(), 5 * MINUTO);
});

test('zerar volta ao tempo cheio, parado', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(4 * MINUTO);
  motor.zerar();
  assert.equal(motor.estado(), 'parado');
  assert.equal(motor.restante(), 10 * MINUTO);
});

test('somar estende sem perder a contagem', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(4 * MINUTO);
  motor.somar(MINUTO);
  assert.equal(motor.restante(), 7 * MINUTO);
});

test('tirar minuto nunca deixa a duração negativa', () => {
  const { motor } = montar();
  motor.carregar(30 * SEGUNDO);
  motor.somar(-MINUTO);
  assert.equal(motor.duracao(), 0);
});

test('contagem negativa ligada segue abaixo de zero', () => {
  const { relogio, motor } = montar({ negativo: true });
  motor.carregar(MINUTO);
  motor.iniciar();
  relogio.avancar(80 * SEGUNDO);
  motor.atualizar();
  assert.equal(motor.estourado(), true);
  assert.equal(motor.restante(), -20 * SEGUNDO);
  assert.equal(motor.estado(), 'contando');
});

test('contagem negativa desligada para em zero', () => {
  const { relogio, motor } = montar({ negativo: false });
  motor.carregar(MINUTO);
  motor.iniciar();
  relogio.avancar(80 * SEGUNDO);
  assert.equal(motor.restante(), 0);
  motor.atualizar();
  assert.equal(motor.estourado(), false);
  assert.equal(motor.restante(), 0);
  assert.equal(motor.estado(), 'parado');
});

test('fim acontece uma vez ao cruzar o zero', () => {
  const { relogio, motor } = montar();
  motor.carregar(30 * SEGUNDO);
  motor.iniciar();
  relogio.avancar(29 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), []);
  relogio.avancar(2 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
  assert.deepEqual(motor.atualizar(), []);
});

test('aviso acontece uma vez no limiar', () => {
  const { relogio, motor } = montar({ avisoEm: 2 * MINUTO });
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  relogio.avancar(7 * MINUTO);
  assert.deepEqual(motor.atualizar(), []);
  relogio.avancar(1.5 * MINUTO);
  assert.deepEqual(motor.atualizar(), ['aviso']);
  assert.deepEqual(motor.atualizar(), []);
});

test('somar tempo que volta ao positivo permite novo fim', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * SEGUNDO);
  motor.iniciar();
  relogio.avancar(11 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
  motor.somar(30 * SEGUNDO);
  relogio.avancar(30 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
});

test('parado ou pausado, atualizar não produz eventos', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * SEGUNDO);
  relogio.avancar(20 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), []);
  motor.iniciar();
  motor.pausar();
  relogio.avancar(20 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), []);
});

test('configurar muda a contagem negativa e o aviso com a página aberta', () => {
  const { relogio, motor } = montar({ negativo: true, avisoEm: null });
  motor.configurar({ negativo: false, avisoEm: MINUTO });
  motor.carregar(2 * MINUTO);
  motor.iniciar();
  relogio.avancar(90 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['aviso']);
  relogio.avancar(60 * SEGUNDO);
  assert.deepEqual(motor.atualizar(), ['fim']);
  assert.equal(motor.restante(), 0);
  assert.equal(motor.estado(), 'parado');
});

test('sem deriva: 6.000 atualizações em 10 minutos terminam exatamente em zero', () => {
  const { relogio, motor } = montar();
  motor.carregar(10 * MINUTO);
  motor.iniciar();
  for (let i = 0; i < 6000; i++) {
    relogio.avancar(100);
    motor.atualizar();
  }
  assert.equal(motor.restante(), 0);
});

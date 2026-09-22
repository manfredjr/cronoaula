// Liga o cronometro a pagina. E o unico modulo que toca no DOM: botoes,
// teclado, tela cheia, modo projecao, tela acesa, ajustes e o laco de desenho.

import { criarMotor } from './motor.js';
import { interpretar, formatar } from './tempo.js';
import { faixa, estadoEscrito } from './alertas.js';
import { criarSom } from './som.js';
import { carregar, salvar, normalizar, PADROES } from './ajustes.js';

const MINUTO = 60000;
const $ = (id) => document.getElementById(id);

const el = {
  digitos: $('digitos'),
  estado: $('estado'),
  iniciar: $('iniciar'),
  zerar: $('zerar'),
  maisUm: $('mais-um'),
  presets: [...document.querySelectorAll('[data-min]')],
  formTempo: $('form-tempo'),
  campo: $('campo-tempo'),
  erro: $('erro-tempo'),
  telaCheia: $('tela-cheia'),
  abrirAjustes: $('abrir-ajustes'),
  barraIniciar: $('barra-iniciar'),
  barraZerar: $('barra-zerar'),
  barraMais: $('barra-mais'),
  barraSair: $('barra-sair'),
  dialogo: $('ajustes'),
  formAjustes: $('form-ajustes'),
  volumeTexto: $('volume-texto'),
  testarAlerta: $('testar-alerta'),
  testarAviso: $('testar-aviso'),
  restaurar: $('restaurar'),
};

// Em janela anonima ou com dados bloqueados, ate ler localStorage pode lancar.
let armazenamento = null;
try {
  armazenamento = window.localStorage;
} catch {
  armazenamento = null;
}

let ajustes = carregar(armazenamento);
const toque = window.matchMedia('(pointer: coarse)').matches;
const temTelaCheia = Boolean(document.documentElement.requestFullscreen && document.fullscreenEnabled);

const motor = criarMotor({
  relogio: () => performance.now(),
  negativo: ajustes.negativo,
  avisoEm: limiarAviso(),
});
const som = criarSom();

let armado = null;        // minutos do tempo rapido carregado, esperando o segundo clique
let avisoPassou = false;  // o aviso antecipado ja tocou ou ficou para tras nesta contagem
let quadroPedido = null;
let trava = null;         // Wake Lock
let pedidoTelaAcesa = null; // pedido de trava em andamento, evita duas travas se o pedido se sobrepuser
let temporizadorBarra = null;
const ultimo = { digitos: '', faixa: '', estado: null, botao: '', armado: undefined };

function limiarAviso() {
  return ajustes.avisoLigado ? ajustes.avisoMinutos * MINUTO : null;
}

// ---- desenho ---------------------------------------------------------------

function desenhar() {
  const restante = motor.restante();
  const estado = motor.estado();
  const f = faixa(restante, motor.duracao());

  const texto = formatar(restante);
  if (texto !== ultimo.digitos) {
    el.digitos.textContent = texto;
    el.digitos.classList.toggle('longo', texto.replace('-', '').length > 5);
    ultimo.digitos = texto;
  }

  if (f !== ultimo.faixa) {
    document.body.dataset.faixa = f;
    ultimo.faixa = f;
  }

  const escrito = estadoEscrito({ estado, faixa: f, armado, toque });
  if (escrito !== ultimo.estado) {
    el.estado.textContent = escrito;
    ultimo.estado = escrito;
  }

  let rotulo = 'Iniciar';
  if (estado === 'contando') rotulo = 'Pausar';
  else if (estado === 'pausado') rotulo = 'Continuar';
  if (rotulo !== ultimo.botao) {
    el.iniciar.textContent = rotulo;
    el.barraIniciar.textContent = rotulo;
    ultimo.botao = rotulo;
  }

  if (armado !== ultimo.armado) {
    for (const b of el.presets) {
      const sim = Number(b.dataset.min) === armado;
      b.classList.toggle('armado', sim);
      b.setAttribute('aria-pressed', String(sim));
    }
    ultimo.armado = armado;
  }
}

// Contando e com a aba visivel, redesenha a cada quadro. Parado, nada roda.
function quadro() {
  quadroPedido = null;
  const eventos = motor.atualizar();
  if (eventos.includes('aviso')) avisoPassou = true;
  if (eventos.includes('fim') && motor.estado() !== 'contando') soltarTelaAcesa();
  desenhar();
  acordar();
}

function acordar() {
  if (quadroPedido === null && motor.estado() === 'contando' && !document.hidden)
    quadroPedido = requestAnimationFrame(quadro);
}

// ---- acoes -------------------------------------------------------------------

function reagendar() {
  if (motor.estado() !== 'contando') return;
  const restante = motor.restante();
  const limiar = limiarAviso();
  if (limiar !== null && restante <= limiar) avisoPassou = true;
  som.agendar({
    fimEmMs: restante,
    avisoEmMs: limiar === null || avisoPassou ? null : restante - limiar,
    repeticoes: ajustes.repeticoes,
    intervaloS: ajustes.intervalo,
    volume: ajustes.volume,
    ligado: ajustes.som,
  });
}

function iniciar() {
  motor.iniciar();
  armado = null;
  reagendar();
  pedirTelaAcesa();
  desenhar();
  acordar();
}

function pausar() {
  motor.pausar();
  som.cancelar();
  soltarTelaAcesa();
  desenhar();
}

function alternar() {
  if (motor.estado() === 'contando') pausar();
  else iniciar();
}

function zerar() {
  motor.zerar();
  armado = null;
  avisoPassou = false;
  som.cancelar();
  soltarTelaAcesa();
  desenhar();
}

function carregarTempo(ms, minutosRapidos = null) {
  motor.carregar(ms);
  armado = minutosRapidos;
  avisoPassou = false;
  som.cancelar();
  soltarTelaAcesa();
  ajustes = { ...ajustes, ultimoMinutos: ms / MINUTO };
  salvar(armazenamento, ajustes);
  desenhar();
}

function tempoRapido(minutos) {
  if (armado === minutos && motor.estado() === 'parado') iniciar();
  else carregarTempo(minutos * MINUTO, minutos);
}

function somar(ms) {
  // Se o aviso ja ficou para tras, somar tempo nao faz ele tocar de novo.
  const limiar = limiarAviso();
  if (motor.estado() === 'contando' && limiar !== null && motor.restante() <= limiar) avisoPassou = true;
  motor.somar(ms);
  ajustes = { ...ajustes, ultimoMinutos: motor.duracao() / MINUTO };
  salvar(armazenamento, ajustes);
  reagendar();
  desenhar();
}

// ---- tela acesa (Wake Lock) ---------------------------------------------------

async function pedirTelaAcesa() {
  if (trava || pedidoTelaAcesa || !('wakeLock' in navigator) || document.hidden) return;
  pedidoTelaAcesa = navigator.wakeLock.request('screen');
  try {
    const nova = await pedidoTelaAcesa;
    if (motor.estado() !== 'contando') {
      nova.release().catch(() => {});
      return;
    }
    trava = nova;
    trava.addEventListener('release', () => { if (trava === nova) trava = null; });
  } catch {
    // sem suporte ou negado: segue sem
  } finally {
    pedidoTelaAcesa = null;
  }
}

function soltarTelaAcesa() {
  if (!trava) return;
  trava.release().catch(() => {});
  trava = null;
}

document.addEventListener('visibilitychange', () => {
  if (document.hidden) return;
  desenhar();
  acordar();
  if (motor.estado() === 'contando') pedirTelaAcesa();
});

// ---- tela cheia e modo projecao ----------------------------------------------

function emTelaCheia() {
  return document.fullscreenElement != null || document.body.classList.contains('projecao');
}

function ativarProjecao(sim) {
  document.body.classList.toggle('projecao', sim);
  if (sim) mostrarBarra();
  else document.body.classList.remove('barra-visivel');
}

function entrarTelaCheia() {
  // O Safari do iPhone nao tem tela cheia fora de video: la vale o modo projecao.
  if (!temTelaCheia) {
    ativarProjecao(true);
    return;
  }
  let assentado = false;
  // Alguns navegadores nunca respondem ao pedido (nem resolvem, nem rejeitam): sem isto o botao trava.
  const semResposta = setTimeout(() => {
    if (!assentado) ativarProjecao(true);
  }, 1500);
  document.documentElement.requestFullscreen()
    .catch(() => ativarProjecao(true))
    .finally(() => {
      assentado = true;
      clearTimeout(semResposta);
    });
}

function sairTelaCheia() {
  if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
  ativarProjecao(false);
}

function alternarTelaCheia() {
  if (emTelaCheia()) sairTelaCheia();
  else entrarTelaCheia();
}

function mostrarBarra() {
  if (!emTelaCheia()) return;
  document.body.classList.add('barra-visivel');
  clearTimeout(temporizadorBarra);
  temporizadorBarra = setTimeout(() => document.body.classList.remove('barra-visivel'), 3000);
}

document.addEventListener('fullscreenchange', () => {
  const cheia = document.fullscreenElement != null;
  document.body.classList.toggle('cheia', cheia);
  if (cheia) mostrarBarra();
  else document.body.classList.remove('barra-visivel');
});

// ---- ajustes -------------------------------------------------------------------

function preencherAjustes() {
  const f = el.formAjustes.elements;
  f.som.checked = ajustes.som;
  f.volume.value = String(Math.round(ajustes.volume * 100));
  f.repeticoes.value = String(ajustes.repeticoes);
  f.intervalo.value = String(ajustes.intervalo);
  f.negativo.checked = ajustes.negativo;
  f.avisoLigado.checked = ajustes.avisoLigado;
  f.avisoMinutos.value = String(ajustes.avisoMinutos);
  el.volumeTexto.textContent = `${Math.round(ajustes.volume * 100)}%`;
}

function lerAjustes() {
  const f = el.formAjustes.elements;
  // Campo vazio ou pela metade mantem o valor atual; o normalizar traz para a faixa.
  const numero = (campo, atual) => {
    const v = Number(campo.value);
    return campo.value.trim() === '' || !Number.isFinite(v) ? atual : v;
  };
  return normalizar({
    ...ajustes,
    som: f.som.checked,
    volume: numero(f.volume, ajustes.volume * 100) / 100,
    repeticoes: numero(f.repeticoes, ajustes.repeticoes),
    intervalo: numero(f.intervalo, ajustes.intervalo),
    negativo: f.negativo.checked,
    avisoLigado: f.avisoLigado.checked,
    avisoMinutos: numero(f.avisoMinutos, ajustes.avisoMinutos),
  });
}

function aplicarAjustes(novos) {
  ajustes = novos;
  salvar(armazenamento, ajustes);
  motor.configurar({ negativo: ajustes.negativo, avisoEm: limiarAviso() });
  reagendar();
  desenhar();
  acordar();
}

el.abrirAjustes.addEventListener('click', () => {
  preencherAjustes();
  el.dialogo.showModal();
});
el.formAjustes.addEventListener('input', () => {
  aplicarAjustes(lerAjustes());
  el.volumeTexto.textContent = `${Math.round(ajustes.volume * 100)}%`;
});
// Ao sair do campo, mostra o valor ja corrigido para dentro da faixa.
el.formAjustes.addEventListener('change', preencherAjustes);
el.restaurar.addEventListener('click', () => {
  aplicarAjustes({ ...PADROES, ultimoMinutos: ajustes.ultimoMinutos });
  preencherAjustes();
});
el.testarAlerta.addEventListener('click', () => som.testarAlerta(ajustes.volume));
el.testarAviso.addEventListener('click', () => som.testarAviso(ajustes.volume));

// ---- botoes, campo e teclado ----------------------------------------------------

// Encostar na pagina ja silencia o alerta de fim, como no programa: se o
// professor mexeu, e porque ja percebeu. Tambem libera o audio no primeiro gesto.
document.addEventListener('pointerdown', () => {
  som.liberar();
  som.silenciar();
  mostrarBarra();
}, true);
document.addEventListener('pointermove', mostrarBarra);

el.iniciar.addEventListener('click', alternar);
el.barraIniciar.addEventListener('click', alternar);
el.zerar.addEventListener('click', zerar);
el.barraZerar.addEventListener('click', zerar);
el.maisUm.addEventListener('click', () => somar(MINUTO));
el.barraMais.addEventListener('click', () => somar(MINUTO));
el.barraSair.addEventListener('click', sairTelaCheia);
el.telaCheia.addEventListener('click', alternarTelaCheia);
for (const b of el.presets)
  b.addEventListener('click', () => tempoRapido(Number(b.dataset.min)));

el.formTempo.addEventListener('submit', (e) => {
  e.preventDefault();
  const ms = interpretar(el.campo.value);
  if (ms === null) {
    el.erro.hidden = false;
    return;
  }
  el.erro.hidden = true;
  carregarTempo(ms);
  el.campo.blur(); // devolve o Espaco e as setas ao cronometro
});
el.campo.addEventListener('input', () => { el.erro.hidden = true; });

document.addEventListener('keydown', (e) => {
  som.liberar();
  som.silenciar();
  if (el.dialogo.open || e.ctrlKey || e.metaKey || e.altKey) return;
  if (e.target === el.campo) return;

  switch (e.key) {
    case ' ':
      // Espaco e sempre iniciar e pausar, mesmo com o foco num botao.
      e.preventDefault();
      if (!e.repeat) alternar();
      break;
    case 'r':
    case 'R':
      zerar();
      break;
    case 'ArrowUp':
      e.preventDefault();
      somar(MINUTO);
      break;
    case 'ArrowDown':
      e.preventDefault();
      somar(-MINUTO);
      break;
    case 'f':
    case 'F':
      alternarTelaCheia();
      break;
    case 'Escape':
      sairTelaCheia();
      break;
    default:
      break;
  }
});

// Sem isto, soltar o Espaco com o foco num botao tambem clicaria nele.
document.addEventListener('keyup', (e) => {
  if (e.key === ' ' && e.target instanceof HTMLButtonElement && !el.dialogo.open) e.preventDefault();
});

// ---- partida -------------------------------------------------------------------

if (toque) el.telaCheia.textContent = 'Tela cheia';
motor.carregar(ajustes.ultimoMinutos * MINUTO);
desenhar();

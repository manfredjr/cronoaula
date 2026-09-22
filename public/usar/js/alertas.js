// Faixas de alerta e a linha de estado embaixo dos digitos.
// Mesmas regras do MainViewModel.ComputeAlert do programa de Windows.

const MINUTO = 60000;

const NOMES = {
  normal: '',
  atencao: 'reta final',
  urgente: 'último minuto',
  estourado: 'tempo excedido',
};

/**
 * Reta final nos ultimos 20% do tempo ou 2 minutos, o que for menor; ultimo
 * minuto no minuto final; tempo excedido ao zerar ou passar do zero.
 */
export function faixa(restanteMs, duracaoMs) {
  if (restanteMs <= 0) return 'estourado';
  if (restanteMs <= MINUTO) return 'urgente';
  const limite = Math.min(duracaoMs * 0.2, 2 * MINUTO);
  return restanteMs <= limite ? 'atencao' : 'normal';
}

export function nome(f) {
  return NOMES[f] ?? '';
}

/**
 * Texto da linha de estado, em ordem de prioridade: pausado; tempo excedido;
 * tempo rapido carregado; nome da faixa; vazio contando no normal; pronto.
 */
export function estadoEscrito({ estado, faixa: f, armado, toque }) {
  if (estado === 'pausado') return 'pausado';
  if (f === 'estourado') return nome(f);
  if (armado !== null && armado !== undefined && estado === 'parado')
    return `${armado} min carregado. ${toque ? 'Toque' : 'Clique'} de novo para iniciar`;
  if (f !== 'normal') return nome(f);
  return estado === 'contando' ? '' : 'pronto';
}

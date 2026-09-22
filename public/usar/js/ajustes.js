// Preferencias guardadas no navegador. O armazenamento vem de fora (na pagina,
// localStorage; nos testes, um falso). Armazenamento bloqueado ou corrompido
// nunca impede a pagina de funcionar: ela usa os padroes e segue.

export const CHAVE = 'cronoaula.ajustes';

export const PADROES = Object.freeze({
  som: true,
  volume: 0.7,
  repeticoes: 5,
  intervalo: 3,
  avisoLigado: true,
  avisoMinutos: 5,
  negativo: true,
  ultimoMinutos: 50,
});

const LIGA_DESLIGA = ['som', 'avisoLigado', 'negativo'];

// chave: [minimo, maximo, inteiro]
const FAIXAS = {
  volume: [0, 1, false],
  repeticoes: [0, 60, true],
  intervalo: [1, 30, true],
  avisoMinutos: [1, 120, true],
};

const numero = (v) => typeof v === 'number' && Number.isFinite(v);

export function normalizar(bruto) {
  const b = bruto && typeof bruto === 'object' ? bruto : {};
  const r = { ...PADROES };

  for (const chave of LIGA_DESLIGA)
    if (typeof b[chave] === 'boolean') r[chave] = b[chave];

  for (const [chave, [min, max, inteiro]] of Object.entries(FAIXAS)) {
    if (!numero(b[chave])) continue;
    const v = inteiro ? Math.round(b[chave]) : b[chave];
    r[chave] = Math.min(max, Math.max(min, v));
  }

  // O ultimo tempo usado nao e trazido para dentro da faixa: fora dela, volta a 50.
  if (numero(b.ultimoMinutos) && b.ultimoMinutos >= 0 && b.ultimoMinutos <= 1440)
    r.ultimoMinutos = b.ultimoMinutos;

  return r;
}

export function carregar(armazenamento) {
  try {
    const texto = armazenamento.getItem(CHAVE);
    if (!texto) return { ...PADROES };
    return normalizar(JSON.parse(texto));
  } catch {
    return { ...PADROES };
  }
}

export function salvar(armazenamento, ajustes) {
  try {
    armazenamento.setItem(CHAVE, JSON.stringify(normalizar(ajustes)));
    return true;
  } catch {
    return false;
  }
}

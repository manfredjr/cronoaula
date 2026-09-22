// Leitura do tempo digitado e formatacao do mostrador.
// Mesmas regras do TimeParser do programa de Windows; os casos em
// casos-compartilhados.json garantem que as duas versoes concordam.

const INTEIRO = /^\d+$/;
const MINUTOS = /^(\d+\.?\d*|\.\d+)$/;

/**
 * Converte o texto digitado em milissegundos.
 * Aceita "MM:SS" (minutos podem passar de 59), "HH:MM:SS" e so minutos,
 * com fracao por ponto ou virgula ("7,5"). Devolve null se nao entender.
 */
export function interpretar(texto) {
  if (typeof texto !== 'string') return null;
  const limpo = texto.trim();
  if (limpo === '') return null;

  if (limpo.includes(':')) {
    const partes = limpo.split(':').map((p) => p.trim());
    if (partes.length < 2 || partes.length > 3) return null;
    if (!partes.every((p) => INTEIRO.test(p))) return null;
    const n = partes.map(Number);

    if (n.length === 2) {
      const [mm, ss] = n;
      if (ss > 59) return null;
      return (mm * 60 + ss) * 1000;
    }

    const [hh, mm, ss] = n;
    if (mm > 59 || ss > 59) return null;
    return ((hh * 60 + mm) * 60 + ss) * 1000;
  }

  const normalizado = limpo.replace(/,/g, '.');
  if (!MINUTOS.test(normalizado)) return null;
  return Math.round(Number(normalizado) * 60000);
}

/**
 * "MM:SS" abaixo de uma hora e "HH:MM:SS" a partir dela, arredondando para o
 * segundo mais proximo. Negativo (tempo excedido) leva "-" na frente.
 */
export function formatar(ms) {
  const negativo = ms < 0;
  const total = Math.round(Math.abs(ms) / 1000);
  const hh = Math.floor(total / 3600);
  const mm = Math.floor((total % 3600) / 60);
  const ss = total % 60;
  const dois = (v) => String(v).padStart(2, '0');
  const corpo = hh >= 1 ? `${dois(hh)}:${dois(mm)}:${dois(ss)}` : `${dois(mm)}:${dois(ss)}`;
  return negativo ? `-${corpo}` : corpo;
}

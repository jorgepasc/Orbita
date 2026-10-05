import type { Ficha } from '../fichas/ficha-api';
import { agruparPorMes, normalizarTexto } from './agrupacion-mensual';

function ficha(parcial: Partial<Ficha> & { id: string; titulo: string; fecha: string }): Ficha {
  return {
    tipo: 'Película',
    terminado: true,
    valoracion: null,
    comentarios: null,
    compania: null,
    plataforma: null,
    idioma: null,
    snapshot: null,
    ...parcial,
  };
}

describe('agruparPorMes', () => {
  it('agrupa por mes con etiqueta en español y meses descendentes', () => {
    const grupos = agruparPorMes([
      ficha({ id: 'enero', titulo: 'Enero', fecha: '2026-01-05' }),
      ficha({ id: 'marzo-1', titulo: 'Marzo uno', fecha: '2026-03-14' }),
      ficha({ id: 'marzo-2', titulo: 'Marzo dos', fecha: '2026-03-02' }),
    ]);

    expect(grupos.map((grupo) => grupo.clave)).toEqual(['2026-03', '2026-01']);
    expect(grupos[0].etiqueta).toBe('marzo 2026');
    expect(grupos[0].fichas.map((f) => f.id)).toEqual(['marzo-1', 'marzo-2']);
  });

  it('desempata por título ascendente cuando comparten fecha', () => {
    const grupos = agruparPorMes([
      ficha({ id: 'zeta', titulo: 'Zeta', fecha: '2026-02-10' }),
      ficha({ id: 'alfa', titulo: 'Alfa', fecha: '2026-02-10' }),
    ]);

    expect(grupos[0].fichas.map((f) => f.id)).toEqual(['alfa', 'zeta']);
  });

  it('ignora fichas sin fecha', () => {
    const sinFecha = { ...ficha({ id: 'sin', titulo: 'Sin', fecha: '2026-02-10' }), fecha: null };
    const grupos = agruparPorMes([sinFecha]);

    expect(grupos).toEqual([]);
  });
});

describe('normalizarTexto', () => {
  it('ignora acentos y mayúsculas', () => {
    expect(normalizarTexto('Ciencia ficción')).toBe('ciencia ficcion');
    expect(normalizarTexto('NIÑO')).toBe('nino');
  });
});

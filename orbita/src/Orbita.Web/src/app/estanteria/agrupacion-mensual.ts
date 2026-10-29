import type { Ficha } from '../fichas/ficha-api';

export interface GrupoMes {
  clave: string;
  etiqueta: string;
  fichas: Ficha[];
}

const FORMATO_MES = new Intl.DateTimeFormat('es', { month: 'long' });

export function normalizarTexto(texto: string): string {
  return texto
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .trim();
}

function parsearFecha(fecha: string): Date {
  const [anio, mes, dia] = fecha.split('-').map(Number);
  return new Date(anio, mes - 1, dia);
}

function etiquetaMes(fecha: string): string {
  const date = parsearFecha(fecha);
  return `${FORMATO_MES.format(date)} ${date.getFullYear()}`;
}

export function agruparPorMes(fichas: Ficha[]): GrupoMes[] {
  const ordenadas = fichas
    .filter((ficha) => ficha.fecha !== null)
    .sort((primera, segunda) => {
      const porFecha = segunda.fecha!.localeCompare(primera.fecha!);
      return porFecha !== 0 ? porFecha : primera.titulo.localeCompare(segunda.titulo);
    });

  const grupos: GrupoMes[] = [];
  for (const ficha of ordenadas) {
    const clave = ficha.fecha!.slice(0, 7);
    let grupo = grupos.at(-1);
    if (!grupo || grupo.clave !== clave) {
      grupo = { clave, etiqueta: etiquetaMes(ficha.fecha!), fichas: [] };
      grupos.push(grupo);
    }
    grupo.fichas.push(ficha);
  }

  return grupos;
}

export function ordenarPorValoracion(fichas: Ficha[]): Ficha[] {
  return [...fichas].sort((primera, segunda) => {
    if (primera.valoracion !== segunda.valoracion) {
      if (primera.valoracion === null) {
        return 1;
      }
      if (segunda.valoracion === null) {
        return -1;
      }
      return segunda.valoracion - primera.valoracion;
    }
    const porFecha = (segunda.fecha ?? '').localeCompare(primera.fecha ?? '');
    return porFecha !== 0 ? porFecha : primera.titulo.localeCompare(segunda.titulo);
  });
}

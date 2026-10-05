import { ESTANTERIAS, estanteriaDeTipo, getEstanteria } from './estanterias';

describe('estanterias', () => {
  it('agrupa los cinco Tipos en tres Estanterías', () => {
    expect(ESTANTERIAS.map((estanteria) => estanteria.nombre)).toEqual([
      'Pantalla',
      'Lecturas',
      'Juegos',
    ]);
    expect(getEstanteria('pantalla').tipos).toEqual(['Película', 'Serie']);
    expect(getEstanteria('lecturas').tipos).toEqual(['Libro', 'Cómic']);
    expect(getEstanteria('juegos').tipos).toEqual(['Juego']);
  });

  it('localiza la Estantería de cada Tipo', () => {
    expect(estanteriaDeTipo('Serie').id).toBe('pantalla');
    expect(estanteriaDeTipo('Cómic').id).toBe('lecturas');
    expect(estanteriaDeTipo('Juego').id).toBe('juegos');
  });

  it('cae en Pantalla ante una Estantería desconocida', () => {
    expect(getEstanteria('desconocida').id).toBe('pantalla');
  });
});

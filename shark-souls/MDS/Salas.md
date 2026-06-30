# Mundo 1:
- [x] Sala inicial:
- [x] Capa superficies:
	- [x] Sala Puerta izquierda y abajo.
	- [x] Sala Puerta derecha y abajo.
	- [x] Izquierda, Derecha y abajo.
- [x] Capa Media: 
	- [x] Izquierda, abajo y arriba.
	- [x] Izquierda, derecha, arriba y abajo.
	- [x] Derecha, abajo y arriba.
- [x] Capa Profunda:
	- [x] Izquierda y arriba.
	- [x] Izquierda, derecha y arriba.
	- [x] Derecha y arriba.
- [x] Sala Boss


- **Estructura del Prefab:** Crea un objeto padre para tu sala. Dentro de él, añade las paredes estables y los fondos.
    
- **Crear los Bloqueadores:** En los umbrales de las puertas (Arriba, Abajo, Izquierda, Derecha), coloca un objeto visual que contenga el **Sprite de una pared sólida** o una compuerta cerrada que tape por completo el camino.
    
- **Vincular en el Inspector:** Selecciona el Prefab de la sala, añade el script `Sala.cs`, marca qué puertas tiene tu diseño arquitectónico (ej. `Tiene Izquierda` y `Tiene Abajo`) y arrastra los objetos visuales correspondientes a las casillas de **Bloqueadores**.

## Camara 

- **Crear una Capa (Layer):** En la esquina superior derecha de Unity, haz clic en _Layers -> Edit Layers_ y crea una capa llamada **`Salas`**.
    
- **Asignar la Capa:** Ve al prefab de tus salas y asegúrate de que el objeto padre (el que tiene el script `Sala.cs`) tenga asignada la Layer **`Salas`**.
    
- **Añadir un Collider a la Sala:** El objeto padre de tu sala debe tener un **`Box Collider 2D`** que cubra exactamente todo el tamaño de la sala (ej. `Size X: 30, Y: 18`) y marcar la casilla **`Is Trigger`** en `true`. Esto servirá como la "zona" que detectará la cámara al pasar el jugador.
    
- **Configurar el script de la Cámara:** Selecciona la _Main Camera_, arrastra al jugador a la casilla `Objetivo Jugador` y en `Capa Salas` selecciona tu capa **`Salas`**.
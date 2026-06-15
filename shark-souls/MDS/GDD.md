# 🦈 GDD Preliminar: Sharkest Dungeon

## 1. Idea de Ascensor (High Concept)
Un juego de acción y combate en 2D donde controlas a un tiburón que debe **nadar y destruir constantemente para sobrevivir**. Combina la perspectiva y el control de un juego de plataformas/exploración con la locura arcade de los clásicos de navegador (*Medieval Shark*) y el sistema de mejoras acumulativas y caóticas de *Brotato* o *Hades*.

## 2. El Núcleo del Juego (Core Gameplay)
* **Movimiento Perpetuo (Mecánica de Asfixia):** El tiburón no puede quedarse quieto. Si se detiene, la barra de oxígeno/flujo disminuye rápidamente. Nadar a toda velocidad o morder enemigos mantiene al tiburón vivo. El movimiento *es* supervivencia.
* **Combate Devastador:** Los controles básicos son un mordisco rápido, una embestida (*dash*) que destruye obstáculos/peces pequeños, y el movimiento libre en el agua.
* **Bucle de Juego:** 1. Entrar a una zona infestada de peces, buceadores o barcos.
  2. Destruir y devorar todo a tu paso para conseguir "Nutrientes" (experiencia/moneda).
  3. Al subir de nivel o superar una oleada, elegir entre 3 habilidades aleatorias que se acumulan para crear combinaciones rotas.

## 3. Sistema de Habilidades (Estilo Roguelite)
Las mejoras se dividen en mutaciones pasivas y ataques activos. Algunos ejemplos para empezar:
* **Mutaciones de Mordisco:** *Dientes de titanio* (rompe armaduras), *Mordisco sangriento* (cura al atacar) o *Marea de dientes* (ataques en área).
* **Mejoras de Embestida:** *Estela de fuego/electricidad* (daña a lo que dejas atrás) o *Ariete* (causa explosiones al chocar con estructuras).
* **Habilidades Pasivas:** *Piel de lija* (daño por contacto a enemigos pequeños) o *Frenesí* (más velocidad cuanta menos vida te quede).

## 4. Estética y Atmósfera (Look & Feel)
* **Estilo Visual:** 2D fluido, con animaciones exageradas y mucha respuesta visual (*screen shake*, explosiones de burbujas, efectos de sangre de corte arcade/caricaturesco).
* **Tono:** Gamberro, exagerado y sumamente divertido. No busca ser un juego de terror marino, sino una fantasía de poder destructivo.


# 🚀 Guía de Configuración: Unity + GitHub (Para el Verano)

Este documento detalla el flujo de trabajo paso a paso para tener vuestro proyecto sincronizado sin romper nada.

## 1. Crear el Repositorio en GitHub
1. Entra en tu cuenta de **GitHub** y haz clic en **"New"** (Nuevo repositorio).
2. Nómbralo (ej. `sharkest-dungeon`).
3. Elige si será **Public** (Público) o **Private** (Privado).
4. **⚠️ CRUCIAL:** En la sección **Add .gitignore**, haz clic en el desplegable y busca **`Unity`**. Esto evitará que se suban gigas de archivos temporales de caché que genera el motor.
5. Haz clic en **Create repository**.

## 2. Clonar el Repositorio localmente
Para no usar comandos de terminal, usad **GitHub Desktop**:
1. Descarga e inicia sesión en GitHub Desktop.
2. Ve a *File -> Clone repository* y selecciona vuestro repositorio de la lista.
3. Elige una ruta local limpia (ej. `C:/ProyectosUnity/sharkest-dungeon/`).

## 3. Crear el Proyecto de Unity
1. Abre **Unity Hub** y selecciona **New Project**.
2. Selecciona la plantilla **2D Core** o **2D (URP)** (Recomendado URP para mejores efectos y partículas de agua).
3. **⚠️ IMPORTANTE:** Al elegir la localización del proyecto, selecciona exactamente la carpeta del repositorio que acabas de clonar. Unity debe crear sus archivos *dentro* de la estructura que Git está trackeando.

## 4. Flujo de Trabajo Diario en Git (Para trabajar a la vez)
Para evitar los temidos conflictos de mezcla (*merge conflicts*) en Unity:
1. **Antes de empezar a programar:** Abre GitHub Desktop y dale a **Fetch origin** (y luego **Pull** si tu amigo ha subido algo). Trabaja siempre sobre la versión más nueva.
2. **Evitar tocar la misma escena:** No abráis ni modifiquéis la misma escena (*.unity*) al mismo tiempo. Es mejor trabajar con escenas separadas o usar **Prefabs** para vuestros scripts y objetos, ya que los archivos de escena de Unity son muy difíciles de fusionar si se pisan.
3. **Al terminar tu sesión:** Abre GitHub Desktop, escribe un título breve para tus cambios en la esquina inferior izquierda (ej. *"Añadido script de movimiento"*), haz clic en **Commit to main** y finalmente en **Push origin**.


# FACTS
## tiene que haber combo de bajas
## logros
## parry coletazo
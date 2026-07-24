# 🎮 La Vaca

¡Bienvenido al repositorio oficial del proyecto! Este documento contiene las reglas de colaboración, la estructura de ramas y las pautas técnicas que todos los miembros del equipo deben seguir rigurosamente.

---

## 🛠️ Configuración Inicial Obligatoria

Antes de realizar cualquier cambio, asegúrate de cumplir con estos requisitos:

1. **Unity Versión:** [Escribe aquí la versión exacta de Unity, ej: 2022.3.Xf1].
2. **Git LFS:** Debes tener instalado Git LFS en tu computadora antes de clonar el proyecto. 
   * Ejecuta en tu terminal: `git lfs install`
3. **Ajustes del Editor:** En Unity, verifica que esté activo el modo de texto:
   * `Edit > Project Settings > Editor > Asset Serialization` -> **Force Text**.
   * `Edit > Project Settings > Editor > Version Control` -> **Visible Meta Files**.

---

## 🌿 Flujo de Ramas (Git Flow)

Está estrictamente prohibido realizar commits directamente en las ramas principales. Trabajamos con la siguiente estructura:

* **`main`**: Versión de producción. Solo contiene builds estables y jugables. No se toca.
* **`develop`**: Rama de integración. Aquí se fusionan todas las características terminadas.
* **`feature/nombre-tarea`**: Ramas de desarrollo. Crea una nueva desde `develop` para cada tarea (ej: `feature/movimiento-jugador`, `feature/interfaz-menu`).
* **`bugfix/nombre-error`**: Ramas para corregir fallos encontrados en `develop`.

---

## 🚀 Proceso para Subir Cambios (Pull Requests)

Para integrar tus cambios al proyecto, debes seguir este protocolo:

1. Asegúrate de que tu juego compila sin errores en tu rama local.
2. Haz `git pull origin develop` en tu rama para resolver conflictos localmente antes de subirla.
3. Sube tu rama a GitHub y abre un **Pull Request (PR)** hacia la rama `develop`.
4. El PR requiere la **revisión y aprobación de al menos 1 compañero** (Code Review) antes de fusionarse.
5. Si el PR tiene conflictos o rompe la compilación, será rechazado automáticamente.

---

## ⚠️ Reglas de Convivencia con Unity y Git

Para evitar los temidos conflictos de mezcla (merge conflicts) en escenas y prefabs:

* **Archivos `.meta`:** Nunca borres, ignores ni olvides subir los archivos `.meta`. Si creas un script o asset, su archivo `.meta` debe ir en el mismo commit.
* **Bloqueo de Escenas:** Avisa al equipo por [Discord/Slack/WhatsApp] antes de abrir y modificar una escena principal (ej: `MainScene`). Evita que dos personas editen la misma escena al mismo tiempo.
* **Estructura en Prefabs:** No edites objetos directamente en la escena si puedes evitarlo. Modifícalos dentro de su respectivo **Prefab** para que los cambios se guarden de forma aislada.
* **Commits Atómicos:** Haz commits pequeños y frecuentes. Un commit debe solucionar una sola cosa (ej: "Añadido sonido de salto", no "Avances de la semana").

---

## 📝 Formato de Commits

Para mantener un historial limpio, usa prefijos en tus mensajes de commit:
* `feat:` Nueva característica (ej: `feat: implementado sistema de inventario`)
* `fix:` Corrección de un error (ej: `fix: corregido bug de caída infinita`)
* `refactor:` Rediseño de código existente sin cambiar su función (ej: `refactor: limpieza de la clase Player`)
* `asset:` Añadidos recursos visuales o sonoros (ej: `asset: importados modelos 3D del enemigo`)
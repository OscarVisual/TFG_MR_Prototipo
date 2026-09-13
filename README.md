# TFG_MR_Prototipo

Prototipo funcional desarrollado para el Trabajo de Final de Grado **“Exploración de la convivencia social en entornos de realidad mixta”**.

El proyecto consiste en una aplicación de realidad mixta para **Meta Quest 3**, desarrollada en **Unity**, que simula una conversación presencial aumentada mediante tarjetas flotantes con información contextual ficticia sobre una persona interlocutora.

## Objetivo del prototipo

El objetivo principal del prototipo es explorar cómo puede afectar la visualización de información superpuesta y sugerencias conversacionales durante una conversación presencial.

La aplicación permite experimentar un escenario en el que el usuario ve el entorno real mediante passthrough y, al detectar la presencia de una persona, aparecen tarjetas digitales asociadas a ella. Estas tarjetas muestran información predefinida como afinidad, intereses, recuerdos recientes y sugerencias para continuar la conversación.

## Funcionalidades principales

- Ejecución standalone en Meta Quest 3.
- Escena de realidad mixta con passthrough.
- Detección anónima de personas mediante cámara passthrough y modelo YOLO/Sentis.
- Aparición de tarjetas flotantes alrededor de la persona detectada.
- Posicionamiento espacial mediante raycast de profundidad real.
- Sistema de fallback a distancia fija si la profundidad no devuelve un punto válido.
- Orientación de las tarjetas hacia el usuario para mantener la legibilidad.
- Sugerencias conversacionales cambiables mediante botones B/Y.
- Ocultación automática de tarjetas cuando se pierde la detección.
- Interfaz limpia, sin elementos de depuración visibles en la versión final.

## Tecnologías utilizadas

- Unity
- Meta XR SDK
- Meta Quest 3
- Passthrough Camera API
- Unity Sentis / Unity Inference Engine
- Modelo YOLO compatible con Sentis
- C#
- Android APK

## Enfoque ético

El prototipo **no realiza reconocimiento facial**, **no identifica personas reales** y **no almacena datos biométricos**.

La detección utilizada es anónima y se limita a identificar la presencia genérica de una persona dentro del campo de visión del usuario. La información mostrada en las tarjetas es ficticia, predefinida o controlada, y se utiliza únicamente con fines académicos para simular un posible escenario futuro de realidad mixta social.

Esta decisión forma parte del enfoque ético del proyecto, centrado en estudiar el impacto social de la información superpuesta sin comprometer la privacidad de personas reales.

## Versión final

La versión final del prototipo corresponde a la release:

**v1.0.0 - Prototipo final TFG**

Enlace a la release: 
https://github.com/OscarVisual/TFG_MR_Prototipo/releases/tag/v1.0.0

Archivo APK final:

**TFG_MR_Prototipo_OscarGarcia_v1.0.0.apk**

Enlace a la APK final:
https://github.com/OscarVisual/TFG_MR_Prototipo/releases/download/v1.0.0/TFG_MR_Prototipo_OscarGarcia_v1.0.0.apk

Esta versión fue utilizada para la validación con usuarios y representa el estado final defendible del prototipo para la entrega del TFG.

## Limitaciones conocidas

- El sistema no reconoce ni identifica personas concretas.
- Los perfiles mostrados son predefinidos.
- Las sugerencias conversacionales no se generan mediante IA en tiempo real.
- La detección puede variar según iluminación, distancia, encuadre y condiciones del entorno.
- El prototipo está diseñado como prueba de concepto académica, no como producto comercial final.

## Autor

**Oscar Garcia Diaz**  
Trabajo de Final de Grado "Exploración de la convivencia social en entornos de realidad mixta"
Diseño Digital y Tecnologías Multimedia  
Universitat Politècnica de Catalunya - Centre de la Imatge i la Tecnologia Multimèdia  
Curso 2025-2026

## Uso y derechos

Este repositorio se publica únicamente con finalidad académica, de revisión y evaluación del Trabajo de Final de Grado.

No se autoriza la copia, distribución, modificación, reutilización ni comercialización del código, assets, APK, documentación o cualquier otro material incluido en este repositorio sin permiso explícito del autor.

Todos los derechos reservados.

© 2026 Oscar Garcia Diaz

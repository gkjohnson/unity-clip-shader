# unity-clip-shader
![example](./Docs/example.gif)

Unity shader and scripts for rendering solid geometry clipped by a plane

## Use

Draws the geometry with a double sided stencil pass and draws a quad afterward to give the illusion of solid geometry.

Add a `ClipRenderer` component to the GameObject to be rendered, and attach a `Clip Plane/Basic` material. The `ClipRenderer` component should be considered a replacement for the built-in `Renderer` component.

## Clip Material Options
**Use World Space**

Whether or not the plane provided to the material should be used in the local object space or in world space.

**Plane Vector**

A vector defining a plane in space. The XYZ vector defines the plane's normal, while the W component defines the distance from the origin.

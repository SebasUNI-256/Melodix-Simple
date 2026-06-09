# Melodix-Simple

Melodix-Simple es un reproductor de musica local hecho con .NET MAUI, Entity Framework Core y SQLite.

Esta primera version esta orientada a Windows y permite:

- elegir una carpeta local para la biblioteca;
- escanear subcarpetas de forma recursiva;
- detectar archivos `.mp4`, `.m4a` y `.flac`;
- guardar la carpeta elegida en SQLite;
- reescanear al iniciar;
- listar pistas locales y reproducirlas;
- mostrar titulo, artista y portada cuando el archivo trae metadatos compatibles.

## Arquitectura

La solucion esta separada en proyectos simples:

- `Melodix.Domain`: entidades puras.
- `Melodix.Application`: contratos, DTOs y servicios de aplicacion.
- `Melodix.Infrastructure`: EF Core, SQLite, repositorios y escaneo de archivos.
- `Melodix.Presentation`: app .NET MAUI, UI, DI y reproduccion en Windows.
- `Melodix.Tests`: pruebas unitarias basicas.

## Requisitos

- Windows 10 u 11
- .NET SDK `10.0.300`
- workload de .NET MAUI instalado

La version del SDK esta fijada en [global.json](./global.json).

## Como probar el proyecto correctamente

1. Restaura dependencias:

```powershell
dotnet restore Melodix.slnx
```

2. Ejecuta las pruebas unitarias:

```powershell
dotnet test .\Melodix.Tests\Melodix.Tests.csproj
```

3. Compila la solucion:

```powershell
dotnet build Melodix.slnx
```

4. Ejecuta la app en Windows:

```powershell
dotnet build .\Melodix.Presentation\Melodix.Presentation.csproj -t:Run -f net10.0-windows10.0.19041.0
```

Tambien puedes abrir `Melodix.slnx` en Visual Studio y ejecutar `Melodix.Presentation` como proyecto de inicio.

## Flujo manual recomendado

1. Abre la app.
2. Si la biblioteca esta vacia, pulsa `Agregar carpeta`.
3. Elige una carpeta que tenga archivos `.mp4`, `.m4a` o `.flac`.
4. Verifica que la lista se llene con las pistas encontradas.
5. Selecciona una pista y prueba `Play/Pause`, siguiente, anterior, aleatorio, repetir y la barra de progreso.
6. Cierra y vuelve a abrir la app para confirmar que la carpeta se conserva y la biblioteca se reescanea.

## Notas

- La base de datos SQLite se crea en el almacenamiento local de la app, no dentro del repositorio.
- `NuGet.Config` usa una cache local en `.nuget/packages`, por eso esa carpeta esta ignorada en Git.

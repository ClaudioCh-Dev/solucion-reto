namespace Entrevista.Api.Common.Errores;

// Un error conocido de la API: status HTTP, código estable para el frontend y título para el usuario.
// El código no cambia aunque cambie el texto, así el cliente puede reaccionar a él sin comparar mensajes.
public record ErrorApi(int Status, string Codigo, string Titulo);

// Catálogo de todos los errores que puede devolver la API. Es el único lugar donde se definen sus textos.
public static class ErroresApi
{
    // Genéricos: se aplican según el status cuando nadie indicó un error específico
    // (validaciones automáticas, 401 del middleware JWT, rutas inexistentes, excepciones no controladas…).
    public static readonly ErrorApi Validacion = new(400, "validacion", "Uno o más datos no son válidos.");
    public static readonly ErrorApi NoAutenticado = new(401, "no_autenticado", "Debes iniciar sesión para continuar.");
    public static readonly ErrorApi SinPermiso = new(403, "sin_permiso", "No tienes permiso para realizar esta acción.");
    public static readonly ErrorApi NoEncontrado = new(404, "no_encontrado", "El recurso solicitado no existe.");
    public static readonly ErrorApi MetodoNoPermitido = new(405, "metodo_no_permitido", "El método HTTP no está permitido en esta ruta.");
    public static readonly ErrorApi Conflicto = new(409, "conflicto", "La operación entra en conflicto con el estado actual.");
    public static readonly ErrorApi TipoNoSoportado = new(415, "tipo_no_soportado", "El tipo de contenido no es compatible. Envía JSON.");
    public static readonly ErrorApi ErrorInterno = new(500, "error_interno", "Ocurrió un error inesperado en el servidor.");
    public static readonly ErrorApi ServicioExterno = new(502, "servicio_externo", "Un servicio externo no respondió correctamente.");

    // Específicos de cada módulo.
    public static readonly ErrorApi CredencialesInvalidas = new(401, "credenciales_invalidas", "Correo o contraseña incorrectos.");
    public static readonly ErrorApi SesionExpirada = new(401, "sesion_expirada", "La sesión expiró. Inicia sesión nuevamente.");
    public static readonly ErrorApi ProductoNoEncontrado = new(404, "producto_no_encontrado", "El producto no existe.");
    public static readonly ErrorApi ImportacionEnConflicto = new(409, "importacion_en_conflicto", "Conflicto al guardar la importación.");
    public static readonly ErrorApi FakeStoreNoDisponible = new(502, "fakestore_no_disponible", "No se pudo importar desde FakeStore API.");

    private static readonly Dictionary<int, ErrorApi> Genericos = new[]
    {
        Validacion, NoAutenticado, SinPermiso, NoEncontrado, MetodoNoPermitido,
        Conflicto, TipoNoSoportado, ErrorInterno, ServicioExterno
    }.ToDictionary(e => e.Status);

    public static ErrorApi PorStatus(int status) =>
        Genericos.GetValueOrDefault(status) ?? (status >= 500 ? ErrorInterno : new ErrorApi(status, "error", "La solicitud no se pudo completar."));

    // Enlace a la sección del RFC 9110 que define cada status (campo "type" de ProblemDetails).
    public static string TipoRfc(int status) => status switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        405 => "https://tools.ietf.org/html/rfc9110#section-15.5.6",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        415 => "https://tools.ietf.org/html/rfc9110#section-15.5.16",
        500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
        _ => "about:blank"
    };
}

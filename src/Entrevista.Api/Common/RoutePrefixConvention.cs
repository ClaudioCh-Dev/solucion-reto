using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Entrevista.Api.Common;

// Antepone un prefijo (ej. "api") a la ruta de todos los controladores.
// Así cada controlador declara solo su parte: [Route("products")] → /api/products.
public class RoutePrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers.SelectMany(c => c.Selectors))
        {
            selector.AttributeRouteModel = selector.AttributeRouteModel is null
                ? _prefix
                : AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
        }
    }
}

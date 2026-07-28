using Microsoft.FeatureManagement;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Gates a route group behind a feature flag (spec R7): deploy is not release.
/// While the flag is off the routes respond 404, so the endpoints ship dark until
/// the feature is accepted (Gate 3) and the flag is flipped on.
/// </summary>
public static class FeatureGateExtensions
{
    public static RouteGroupBuilder RequireFeature(this RouteGroupBuilder group, string feature)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            var featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManager>();
            if (!await featureManager.IsEnabledAsync(feature))
                return Results.NotFound();

            return await next(context);
        });

        return group;
    }
}

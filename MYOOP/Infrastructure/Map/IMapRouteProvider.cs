﻿using OOP.Domain.Entities;

namespace OOP.Infrastructure.Map
{
    public interface IMapRouteProvider
    {
        Task<Route?> GetRouteAsync(GeoLocation start, GeoLocation end);
    }
}

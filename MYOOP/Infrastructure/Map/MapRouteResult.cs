﻿using DomainLocation = OOP.Domain.Entities.Location;

namespace OOP.Infrastructure.Map
{
    public class MapRouteResult
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
        public List<DomainLocation> Points { get; set; } = new();
    }
}

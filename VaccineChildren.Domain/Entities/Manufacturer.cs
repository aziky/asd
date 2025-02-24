﻿namespace VaccineChildren.Domain.Entities;

public partial class Manufacturer
{
    public Guid ManufacturerId { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;

    public string? ShortName { get; set; }

    public string? Description { get; set; }

    public string? CountryName { get; set; }

    public string? CountryCode { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual ICollection<VaccineManufacture> VaccineManufactures { get; set; } = new List<VaccineManufacture>();
}

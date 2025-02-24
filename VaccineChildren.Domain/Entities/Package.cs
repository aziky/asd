﻿namespace VaccineChildren.Domain.Entities;

public partial class Package
{
    public Guid PackageId { get; set; } = Guid.NewGuid();

    public string? PackageName { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public decimal? Discount { get; set; }

    public bool? IsActive { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? Unit { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<UserCart> UserCarts { get; set; } = new List<UserCart>();

    public virtual ICollection<Vaccine> Vaccines { get; set; } = new List<Vaccine>();


}

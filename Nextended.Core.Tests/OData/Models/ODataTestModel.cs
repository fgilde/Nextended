using System;

namespace Nextended.Core.Tests.OData.Models;

public class ODataTestModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int IntField { get; set; }
    public string StringField { get; set; } = string.Empty;
    public decimal DecimalField { get; set; }
    public bool BoolField { get; set; }
    public DateTime DateField { get; set; }
    public ODataTestEnum TestEnum { get; set; }   
}
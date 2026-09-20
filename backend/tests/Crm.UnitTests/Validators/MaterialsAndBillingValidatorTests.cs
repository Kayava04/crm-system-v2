using Billing.Application.Features.CreateInvoice;
using Billing.Application.Features.CreatePayroll;
using Materials.Application.Features.CreateMaterial;
using Materials.Domain.Enums;

namespace Crm.UnitTests.Validators;

public class MaterialsAndBillingValidatorTests
{
    private static CreateMaterialRequest Material(MaterialType type, string title = "Title", string? body = null, string? url = null, string? description = null) =>
        new(Guid.NewGuid(), type, title, description, body, url);

    [Fact]
    public void Video_needs_a_youtube_link()
    {
        var validator = new CreateMaterialValidator();

        Assert.True(validator.Validate(Material(MaterialType.Video, url: "https://youtu.be/dQw4w9WgXcQ")).IsValid);
        Assert.False(validator.Validate(Material(MaterialType.Video, url: "https://vimeo.com/123")).IsValid);
        Assert.False(validator.Validate(Material(MaterialType.Video, url: null)).IsValid);
    }

    [Fact]
    public void Article_needs_text_up_to_20000_characters()
    {
        var validator = new CreateMaterialValidator();

        Assert.True(validator.Validate(Material(MaterialType.Article, body: "text")).IsValid);
        Assert.False(validator.Validate(Material(MaterialType.Article, body: "")).IsValid);
        Assert.True(validator.Validate(Material(MaterialType.Article, body: new string('x', 20000))).IsValid);
        Assert.False(validator.Validate(Material(MaterialType.Article, body: new string('x', 20001))).IsValid);
    }

    [Fact]
    public void Link_needs_an_http_address()
    {
        var validator = new CreateMaterialValidator();

        Assert.True(validator.Validate(Material(MaterialType.Link, url: "https://example.com")).IsValid);
        Assert.False(validator.Validate(Material(MaterialType.Link, url: "javascript:alert(1)")).IsValid);
        Assert.False(validator.Validate(Material(MaterialType.Link, url: "example.com")).IsValid);
    }

    [Fact]
    public void Title_is_required_and_limited()
    {
        var validator = new CreateMaterialValidator();

        Assert.False(validator.Validate(Material(MaterialType.Link, title: "", url: "https://a.b")).IsValid);
        Assert.False(validator.Validate(Material(MaterialType.Link, title: new string('x', 201), url: "https://a.b")).IsValid);
    }

    [Fact]
    public void Unknown_type_is_rejected()
    {
        Assert.False(new CreateMaterialValidator().Validate(Material((MaterialType)99, url: "https://a.b")).IsValid);
    }

    [Theory]
    [InlineData("2030-01", true)]
    [InlineData("2030-12", true)]
    [InlineData("2030-00", false)]
    [InlineData("2030-13", false)]
    [InlineData("2030-1", false)]
    [InlineData("30-01", false)]
    [InlineData("2030/01", false)]
    [InlineData("", false)]
    public void Invoice_and_payroll_period_is_yyyy_mm(string period, bool valid)
    {
        var invoice = new CreateInvoiceRequest(Guid.NewGuid(), period, new DateOnly(2030, 2, 1), null);
        var payroll = new CreatePayrollRequest(Guid.NewGuid(), period);

        Assert.Equal(valid, new CreateInvoiceValidator().Validate(invoice).IsValid);
        Assert.Equal(valid, new CreatePayrollValidator().Validate(payroll).IsValid);
    }

    [Fact]
    public void Invoice_and_payroll_need_their_owner()
    {
        Assert.False(new CreateInvoiceValidator().Validate(new CreateInvoiceRequest(Guid.Empty, "2030-01", new DateOnly(2030, 2, 1), null)).IsValid);
        Assert.False(new CreatePayrollValidator().Validate(new CreatePayrollRequest(Guid.Empty, "2030-01")).IsValid);
    }
}

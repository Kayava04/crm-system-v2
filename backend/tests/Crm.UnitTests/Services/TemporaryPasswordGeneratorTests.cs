using Identity.Application.Services;

namespace Crm.UnitTests.Services;

public class TemporaryPasswordGeneratorTests
{
    [Fact]
    public void Password_has_twelve_characters_and_every_required_class()
    {
        for (var i = 0; i < 200; i++)
        {
            var password = TemporaryPasswordGenerator.Generate();

            Assert.Equal(12, password.Length);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, c => !char.IsLetterOrDigit(c));
        }
    }

    [Fact]
    public void Passwords_are_different_each_time()
    {
        var passwords = Enumerable.Range(0, 100).Select(_ => TemporaryPasswordGenerator.Generate()).ToHashSet();

        Assert.Equal(100, passwords.Count);
    }

    [Fact]
    public void Passwords_avoid_easily_confused_characters()
    {
        var all = string.Concat(Enumerable.Range(0, 300).Select(_ => TemporaryPasswordGenerator.Generate()));

        Assert.DoesNotContain('0', all);
        Assert.DoesNotContain('1', all);
        Assert.DoesNotContain('O', all);
        Assert.DoesNotContain('I', all);
        Assert.DoesNotContain('l', all);
    }
}

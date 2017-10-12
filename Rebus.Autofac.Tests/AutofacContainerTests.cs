using NUnit.Framework;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Autofac.Tests
{
    [TestFixture]
    public class AutofacContainerTests : ContainerTests<AutofacActivationContext>
    {
    }

    [TestFixture]
    public class NewAutofacContainerTests : ContainerTests<NewAutofacActivationContext>
    {
    }
}

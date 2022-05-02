using Autofac;
using CliFx.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Migration;
using Recyclarr.Migration.Steps;

namespace Recyclarr.Tests.Migration;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MigrationExecutorTest
{
    [Test]
    public void Migration_steps_are_in_expected_order()
    {
        var container = CompositionRoot.Setup();
        var steps = container.Resolve<IEnumerable<IMigrationStep>>();
        var orderedSteps = steps.OrderBy(x => x.Order).Select(x => x.GetType()).ToList();
        orderedSteps.Should().BeEquivalentTo(
            new[]
            {
                typeof(MigrateTrashYml),
                typeof(MigrateTrashUpdaterAppDataDir)
            },
            config => config.WithStrictOrdering());
    }

    [Test]
    public void Step_not_executed_if_check_returns_false()
    {
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, Substitute.For<IConsole>());

        step.CheckIfNeeded().Returns(false);

        executor.PerformAllMigrationSteps();

        step.Received().CheckIfNeeded();
        step.DidNotReceive().Execute();
    }

    [Test]
    public void Step_executed_if_check_returns_true()
    {
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, Substitute.For<IConsole>());

        step.CheckIfNeeded().Returns(true);

        executor.PerformAllMigrationSteps();

        step.Received().CheckIfNeeded();
        step.Received().Execute();
    }

    [Test]
    public void Steps_executed_in_ascending_order()
    {
        var steps = new[]
        {
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>(),
            Substitute.For<IMigrationStep>()
        };

        steps[0].Order.Returns(20);
        steps[1].Order.Returns(10);
        steps[2].Order.Returns(30);

        var executor = new MigrationExecutor(steps, Substitute.For<IConsole>());

        executor.PerformAllMigrationSteps();

        Received.InOrder(() =>
        {
            steps[1].CheckIfNeeded();
            steps[0].CheckIfNeeded();
            steps[2].CheckIfNeeded();
        });
    }

    [Test]
    public void Exception_converted_to_migration_exception()
    {
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, Substitute.For<IConsole>());

        step.CheckIfNeeded().Returns(true);
        step.When(x => x.Execute()).Throw(new ArgumentException("test message"));

        var act = () => executor.PerformAllMigrationSteps();

        act.Should().Throw<MigrationException>().Which.OriginalException.Message.Should().Be("test message");
    }

    [Test]
    public void Migration_exceptions_are_not_converted()
    {
        var step = Substitute.For<IMigrationStep>();
        var executor = new MigrationExecutor(new[] {step}, Substitute.For<IConsole>());
        var exception = new MigrationException(new Exception(), "a", new[] {"b"});

        step.CheckIfNeeded().Returns(true);
        step.When(x => x.Execute()).Throw(exception);

        var act = () => executor.PerformAllMigrationSteps();

        act.Should().Throw<MigrationException>().Which.Should().Be(exception);
    }
}

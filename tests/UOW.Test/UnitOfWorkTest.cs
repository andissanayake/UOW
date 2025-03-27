﻿using FluentAssertions;

namespace UOW.Test
{
    public class UnitOfWorkTest(ContextFixture contextFixture) : IClassFixture<ContextFixture>
    {
        [Fact]
        public async void CommitTest()
        {
            using var unitOfWork = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork.InsertAsync(item);
            unitOfWork.Commit();

            using var unitOfWork1 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var data = await unitOfWork1.GetAsync<YourEntity1>(item.Id);
            data.Should().NotBeNull(null);
        }

        [Fact]
        public async void RollbackTest()
        {
            using var unitOfWork = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork.InsertAsync(item);
            unitOfWork.Rollback();

            using var unitOfWork1 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var data = await unitOfWork1.GetAsync<YourEntity1>(item.Id);
            data.Should().BeNull(null);
        }

        [Fact]
        public async void MultipleCommitTest()
        {
            using var unitOfWork = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork.InsertAsync(item);
            unitOfWork.Commit();

            using var unitOfWork1 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item1 = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork1.InsertAsync(item1);
            unitOfWork1.Commit();

            using var unitOfWork2 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item2 = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork2.InsertAsync(item2);
            unitOfWork2.Commit();

            using var unitOfWork3 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var list = await unitOfWork3.GetAllAsync<YourEntity1>();
            list.First(i => i.Id == item.Id).Should().NotBeNull();
            list.First(i => i.Id == item1.Id).Should().NotBeNull();
            list.First(i => i.Id == item2.Id).Should().NotBeNull();
        }

        [Fact]
        public async void MultipleRollbackTest()
        {
            using var unitOfWork = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item = new YourEntity1() { Prop1 = "MultipleRollbackTest1", Prop2 = "p2" };
            await unitOfWork.InsertAsync(item);
            unitOfWork.Rollback();

            using var unitOfWork1 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item1 = new YourEntity1() { Prop1 = "MultipleRollbackTest2", Prop2 = "p2" };
            await unitOfWork1.InsertAsync(item1);
            unitOfWork1.Rollback();

            using var unitOfWork2 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item2 = new YourEntity1() { Prop1 = "MultipleRollbackTest3", Prop2 = "p2" };
            await unitOfWork2.InsertAsync(item2);
            unitOfWork2.Rollback();

            using var unitOfWork3 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var list = await unitOfWork3.GetAllAsync<YourEntity1>();
            list.FirstOrDefault(i => i.Prop1 == item.Prop1).Should().BeNull();
            list.FirstOrDefault(i => i.Prop1 == item1.Prop1).Should().BeNull();
            list.FirstOrDefault(i => i.Prop1 == item2.Prop1).Should().BeNull();
        }

        [Fact]
        public async void MultipleCommitRollbackTest()
        {
            using var unitOfWork = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork.InsertAsync(item);
            unitOfWork.Commit();

            using var unitOfWork1 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item1 = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork1.InsertAsync(item1);
            unitOfWork1.Rollback();

            using var unitOfWork2 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var item2 = new YourEntity1() { Prop1 = "p1", Prop2 = "p2" };
            await unitOfWork2.InsertAsync(item2);
            unitOfWork2.Commit();

            using var unitOfWork3 = new UnitOfWork(contextFixture.GetDbConnection(), contextFixture.CurrentUserService);
            var list = await unitOfWork3.GetAllAsync<YourEntity1>();
            list.FirstOrDefault(i => i.Id == item.Id).Should().NotBeNull();
            list.FirstOrDefault(i => i.Id == item1.Id).Should().BeNull();
            list.FirstOrDefault(i => i.Id == item2.Id).Should().NotBeNull();
        }
    }
}

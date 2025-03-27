using Microsoft.AspNetCore.Mvc;

namespace UOW.UI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class YourEntity2Controller : ControllerBase
    {

        private readonly ILogger<YourEntity2Controller> _logger;
        private IUnitOfWorkFactory _unitOfWorkFactory;
        public YourEntity2Controller(ILogger<YourEntity2Controller> logger, IUnitOfWorkFactory unitOfWorkFactory)
        {
            _logger = logger;
            _unitOfWorkFactory = unitOfWorkFactory;
        }

        [HttpGet(Name = "GetYourEntity2")]
        public async Task<IEnumerable<YourEntity2>> GetYourEntity2()
        {
            using var uow = _unitOfWorkFactory.CreateUOW();
            var insertTask1 = uow.InsertAsync(new YourEntity2() { Prop1 = DateTime.Now.ToString(), });
            var insertTask2 = uow.InsertAsync(new YourEntity2() { Prop1 = DateTime.Now.ToString(), });
            var insertTask3 = uow.InsertAsync(new YourEntity2() { Prop1 = DateTime.Now.ToString(), });
            await Task.WhenAll(insertTask1, insertTask2, insertTask3);
            uow.Commit();

            using var uow1 = _unitOfWorkFactory.CreateUOW();
            var itemList = await uow1.GetAllAsync<YourEntity2>();
            var item = itemList.First();
            item.Prop1 = "updated";
            await uow1.UpdateAsync(item);
            uow1.Commit();

            using var uow2 = _unitOfWorkFactory.CreateUOW();
            return await uow2.GetAllAsync<YourEntity2>();
        }

        [HttpPost(Name = "BulkCopy_Guid")]
        public async Task<IEnumerable<YourEntity2>> BulkCopy_Guid()
        {
            using var uow = _unitOfWorkFactory.CreateUOW();
            // Generate 1000 entities
            var list = Enumerable.Range(1, 1000).Select(i => new YourEntity2
            {
                Prop1 = $"Entity {i}",
            }).ToList();

            // Bulk insert into the table
            await uow.BulkCopyAsync(list);
            return await uow.GetAllAsync<YourEntity2>();
        }

        [HttpPost(Name = "BulkCopy_Int")]
        public async Task<IEnumerable<YourEntity1>> BulkCopy_Int()
        {
            using var uow = _unitOfWorkFactory.CreateUOW();
            // Generate 1000 entities
            var list = Enumerable.Range(1, 1000).Select(i => new YourEntity1
            {
                Prop1 = $"Prop1 {i}",
                Prop2 = $"Prop2 {i}",
            }).ToList();

            // Bulk insert into the table
            await uow.BulkCopyAsync(list);
            return await uow.GetAllAsync<YourEntity1>();
        }
    }
}

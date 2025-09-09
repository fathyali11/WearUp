namespace WearUp.Web.Controllers;

[Authorize(Roles =UserRoles.Admin)]
public class MachineLearningController(IModelTrainerRepository _modelTrainerRepository):Controller
{
    [HttpGet]
    public IActionResult Train()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excute()
    {
        await _modelTrainerRepository.EnqueueModelToTrain();
        return Ok();
    }
}

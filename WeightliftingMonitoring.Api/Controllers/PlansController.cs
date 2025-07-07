using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace WeightliftingMonitoring.Api.Controllers;
    [Route("plans")] 
    [ApiController] 
    public class PlansController : ControllerBase
    {
 
        private readonly FirestoreDb _firestoreDb;

        public PlansController(IConfiguration configuration)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                Path.Combine(Directory.GetCurrentDirectory(),"firebase-adminsdk.json"));
            _firestoreDb = FirestoreDb.Create("training-monitoring-4ea8a");
        }
        [HttpGet] 
        public async Task<ActionResult<List<object>>> Get()
        {
            var plansRef = _firestoreDb.Collection("plans");
            var snapshot = await plansRef.GetSnapshotAsync();

            var plans = new List<Dictionary<string, object>>();
            foreach (var doc in snapshot.Documents)
            {
                var planData = doc.ToDictionary();
                planData["id"] = doc.Id;
                plans.Add(planData);
                
            }

            return Ok(plans);
        }

        [HttpPost]
        public void Post([FromBody] string value)
        {
            Console.WriteLine(value);
        }
        
        
    }

// using GaSchedule;
// using GaSchedule.Algorithm;
// using GaSchedule.Model;

// class Program
// {
//     static void Main(string[] args)
//     {
//         var configuration = new Configuration();
//         configuration.ParseFile("./user_data.json");

//         // For HTML output
//         var alg = new Cso<Schedule>(new Schedule(configuration));
//         alg.Run();
//         var htmlResult = HtmlOutput.GetResult(alg.Result);

//         // For JSON output
//         var algJson = new Ngra<Schedule>(new Schedule(configuration));
//         algJson.Run();
//         var jsonResult = JsonOutput.GetResult(algJson.Result);

//         // Output results (for demonstration purposes)
//         Console.WriteLine(htmlResult);
//         Console.WriteLine(jsonResult);
//     }
// }

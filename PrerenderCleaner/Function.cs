using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PrerenderCleaner
{
    public class Function
    {
        readonly static string BASE_PATH = "https://prerender.io";
        readonly static string LOGIN_PATH = BASE_PATH + "/login";
        public static IConfiguration Configuration { get; private set; }

        static Function()
        {
            ConfigureSettings();
        }


        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string FunctionHandler(string input, ILambdaContext context)
        {
            return input?.ToUpper();
        }

        public void StartCleaning(ILambdaContext context)
        {
            var path = GetCurrentPath();
            var driver = new FirefoxDriver(); //new ChromeDriver(new ChromeOptions() { BinaryLocation = path});
            driver.Url = BASE_PATH;
            if (!CanFindTable(driver))
            {
                LoginUsingCreds(driver);
            }
            Thread.Sleep(1000);
            if(CanFindTable(driver))
            {
                RunRemovalScript(driver);
                Thread.Sleep(1000);
                RunRecachingScript(driver);
            }

        }

        


        private void LoginUsingCreds(FirefoxDriver driver)
        {
            do
            {
                driver.Url = LOGIN_PATH;

                var userNameField = driver.FindElement(By.Name("username"));
                var passswordField = driver.FindElement(By.Name("password"));

                userNameField.SendKeys(Configuration["userName"]);
                passswordField.SendKeys(Configuration["password"]);

                driver.ExecuteScript("$('button[type=\"submit\"]').click()");
                Thread.Sleep(1000);
            } while (driver.Url==LOGIN_PATH);
        }


        private void RunRemovalScript(FirefoxDriver driver)
        {
            driver.ExecuteScript(@"$('tr').filter(function(){var content=this.innerText;var datePart=content.match(/\d{2}-\d{2}-\d{4}/g);if(!datePart)return false;var today=moment();var urlDate=moment(datePart,'DD-MM-YYYY');return today.diff(urlDate,'days')>=8;}).each(function(){$(this).find('td.checks').click();});");
            Thread.Sleep(500);
            driver.ExecuteScript(@"$('button[ng-click=""removeSelected()""]').click();");
        }
        private void RunRecachingScript(FirefoxDriver driver)
        {
            driver.ExecuteScript(@"$('tr').filter(function(){var content=this.innerText;return!/\d{2}-\d{2}-\d{4}/g.test(content);}).each(function(){$(this).find('td.checks').click();});");
            Thread.Sleep(500);
            driver.ExecuteScript(@"$('button[ng-click=""cacheSelected()""]').click();");
        }

        private bool CanFindTable(FirefoxDriver driver)
        {
            try
            {
                var tr = driver.FindElement(By.CssSelector("tr"));
                return true;
            }
            catch (OpenQA.Selenium.NoSuchElementException ex)
            {
                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string GetCurrentPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static void ConfigureSettings()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", optional: true)
                .Build();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace WebApplication3
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceController : ControllerBase
    {
        // Метод для получения HTML-кода страницы через Selenium
        private async Task<string> GetPageHtmlWithSelenium(string url)
        {
            var options = new ChromeOptions();
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-images");
            //options.AddArgument("--blink-settings=imagesEnabled=false");
            //options.AddArgument("--disable-animations");

            using (IWebDriver driver = new ChromeDriver(options))
            {
               

                driver.Navigate().GoToUrl(url);

                // Прокручиваем страницу для загрузки всех элементов
                var scrollHeight = Convert.ToInt32(((IJavaScriptExecutor)driver).ExecuteScript("return document.body.scrollHeight"));
                for (int i = 0; i < scrollHeight; i += 300)
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript($"window.scrollTo(0, {i});");
                    await Task.Delay(200);
                }
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(120);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(240);

                // Возвращаем HTML-код страницы
                return driver.PageSource;
            }
        }

        // Метод для извлечения цен с HTML-страницы
        private List<string> ExtractPrices(string html)
        {
            var prices = new List<string>();
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Ищем элементы с ценами
            var priceNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'flat-prices__block-current')]");

            if (priceNodes != null)
            {
                foreach (var node in priceNodes)
                {
                    prices.Add(node.InnerText.Trim());
                }
            }

            return prices;
        }

        // HTTP метод для получения цен по URL
        [HttpGet("get-price")]
        public async Task<ActionResult<List<string>>> GetPrice(string url)
        {
            try
            {
                var html = await GetPageHtmlWithSelenium(url);
                var prices = ExtractPrices(html);
                return Ok(prices);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}

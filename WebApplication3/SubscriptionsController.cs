using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;

namespace WebApplication3
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubscriptionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] Subscription subscription)
        {
            // Получите текущую цену (здесь можно использовать ваш метод)
            var html = await GetPageHtmlWithSelenium(subscription.ApartmentUrl);
            var prices = ExtractPrices(html);
            subscription.CurrentPrice = prices;

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private async Task<string> GetPageHtmlWithSelenium(string url)
        {
            var options = new ChromeOptions();
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-images");
            //options.AddArgument("--blink-settings=imagesEnabled=false");
           // options.AddArgument("--disable-animations");

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
        [HttpGet("GetCurrentPrices")]
        public async Task<IActionResult> GetCurrentPrices()
        {
            // Извлекаем все подписки из базы данных
            var subscriptions = await _context.Subscriptions.ToListAsync();

            // Формируем результат с URL и актуальными ценами
            var result = subscriptions.Select(sub => new
            {
                email=sub.Email,
                ApartmentUrl = sub.ApartmentUrl,
                CurrentPrice = sub.CurrentPrice
            }).ToList();

            // Возвращаем результат
            return Ok(result);
        }
    }
}

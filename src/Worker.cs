using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace src
{
    public class Worker : BackgroundService
    {
        private const string TARGET_URL = "https://www.epicgames.com/store/tr/free-games";
        private const string CHROME_DRIVER_PATH = "YOUR_CHROME_DRIVER_PATH";
        private const string GAME_NAME_SPAN_XPATH = "//*[@id=\"dieselReactWrapper\"]/div/div[4]/main/div/div[3]/div/div/div[2]/div[2]/div/div/section/div/div[1]/div/div/a/div/div/div[3]/span[1]";
        private ChromeOptions _options = new ChromeOptions();
        private ChromeDriverService _service = ChromeDriverService.CreateDefaultService(CHROME_DRIVER_PATH);
        private readonly IServiceProvider _services;
        private FGRContext _dbContext;

        public Worker(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _options.AddArgument("--headless");
                _service.SuppressInitialDiagnosticInformation = true;
                _service.HideCommandPromptWindow = true;

                using (IWebDriver driver = new ChromeDriver(_service, _options))
                {
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    driver.Navigate().GoToUrl(TARGET_URL);
                    Thread.Sleep(5000);
                    var gameNameElement = driver.FindElement(By.XPath(GAME_NAME_SPAN_XPATH));

                    if (gameNameElement != null)
                    {
                        using (var scope = _services.CreateScope())
                        {
                            _dbContext = scope.ServiceProvider.GetRequiredService<FGRContext>();
                            var isGameWasRemindedBefore = await _dbContext.Game.Where(x => x.GameName == gameNameElement.Text).AnyAsync();

                            if (!isGameWasRemindedBefore)
                            {
                                var accountSid = "YOUR_TWILIO_ACCOUNT_SID";
                                var authToken = "YOUR_TWILIO_ACCOUNT_SID";
                                TwilioClient.Init(accountSid, authToken);

                                var messageOptions = new CreateMessageOptions(new PhoneNumber("whatsapp:RECEIVER_NUMBER"));
                                messageOptions.From = new PhoneNumber("whatsapp:SENDER_NUMBER");
                                messageOptions.Body = "Free game oh the week: " + gameNameElement.Text;
                                var message = MessageResource.Create(messageOptions);

                                using (var client = new SmtpClient())
                                {
                                    var mail = new MailMessage();
                                    mail.IsBodyHtml = true;
                                    mail.To.Add("RECEIVER_MAIL");
                                    mail.From = new MailAddress("SENDER_MAIL");
                                    mail.Subject = "Free Game";
                                    mail.Body = "Free game of the week " + gameNameElement.Text;

                                    client.Host = "HOST";
                                    client.Port = 587;
                                    client.EnableSsl = true;
                                    client.Credentials = new NetworkCredential("USERNAME", "PASSWORD");

                                    try
                                    {
                                        client.Send(mail);

                                        Game game = new Game
                                        {
                                            Id = Guid.NewGuid(),
                                            GameName = gameNameElement.Text,
                                            Date = DateTime.Now.Date
                                        };

                                        await _dbContext.Game.AddAsync(game);
                                        await _dbContext.SaveChangesAsync();
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                    }

                    driver.Quit();
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
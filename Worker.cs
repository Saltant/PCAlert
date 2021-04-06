using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Configuration;

namespace PCAlert
{
	public class Worker : BackgroundService
    {
        readonly Ping ping = new();
        readonly string ip;
        readonly int timeout;
        readonly int maxLoss;
        int currentLoss;
        IPStatus status;
        public Worker(IConfiguration configuration)
        {
            ip = configuration.GetSection("Config")["IPAddress"];
            timeout = int.Parse(configuration.GetSection("Config")["Timeout"]);
            maxLoss = int.Parse(configuration.GetSection("Config")["MaxLoss"]);
        }

		
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (currentLoss >= maxLoss)
                    await Alert();
				else
                {
                    var result = await ping.SendPingAsync(ip, timeout);
                    if (result.Status == IPStatus.Success)
                        currentLoss = 0;
					else
					{
                        status = result.Status;
                        currentLoss++;
					}
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

		async Task Alert()
		{
            while (currentLoss != 0)
            {
                Console.Beep();
                MessageBox.Show($"Связь с адресом {ip} потеряна!\nСтатус: {status}", "Внимание!", MessageBoxButtons.Ok, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxModal.System);
                await LastCheckPing();
            }
		}

        async Task<bool> LastCheckPing()
		{
            await Task.Delay(5000);
            bool isPingSuccess = false;
            var result = await ping.SendPingAsync(ip, timeout);
            if (result.Status == IPStatus.Success)
            {
                isPingSuccess = true;
                currentLoss = 0;
            }
            return await Task.FromResult(isPingSuccess);
        }
	}
}

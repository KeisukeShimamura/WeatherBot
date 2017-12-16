using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

using System.Net.Http;
using Newtonsoft.Json;
using AdaptiveCards;
using WeatherBot.Models;

namespace WeatherBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // 返信メッセージを作成
            var message = context.MakeMessage();
            // 天気を取得
            WeatherModel weather = await GetWeatherAsync();

            // 天気をメッセージにセット
            message.Text = $"今日の天気は {weather.forecasts[0].telop.ToString()} です";

            // 返信メッセージをPost
            await context.PostAsync(message);
            context.Wait(MessageReceivedAsync);
        }

        private async Task<WeatherModel> GetWeatherAsync()
        {
            // API から天気情報を取得 (都市コード 140010 (横浜) の場合)
            var client = new HttpClient();
            var weatherResult = await client.GetStringAsync("http://weather.livedoor.com/forecast/webservice/json/v1?city=140010");

            // API 取得したデータをデコードして WeatherModel に取得
            weatherResult = Uri.UnescapeDataString(weatherResult);
            var weatherModel = JsonConvert.DeserializeObject<WeatherModel>(weatherResult);
            return weatherModel;
        }
    }
}
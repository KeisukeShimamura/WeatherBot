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

            //// 天気をメッセージにセット
            //message.Text = $"今日の天気は {weather.forecasts[0].telop.ToString()} です";

            // 取得した天気情報をカードにセット
            var weatherCard = GetCard(weather);
            var attachment = new Attachment()
            {
                Content = weatherCard,
                ContentType = "application/vnd.microsoft.card.adaptive",
                Name = "Weather Forecast"
            };
            message.Attachments.Add(attachment);

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

        private static AdaptiveCard GetCard(WeatherModel model)
        {
            var card = new AdaptiveCard();
            //AddCurrentWeather(model, card);
            AddWeather(model, card);
            return card;
        }

        private static void AddCurrentWeather(WeatherModel model, AdaptiveCard card)
        {
            // タイトル作成
            var titleColumnSet = new ColumnSet();
            card.Body.Add(titleColumnSet);

            var titleColumn = new Column();
            titleColumnSet.Columns.Add(titleColumn);

            var locationText = new TextBlock()
            {
                Text = $"{model.location.city} の天気",
                Size = TextSize.ExtraLarge,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleColumn.Items.Add(locationText);

            // 本文作成
            // 天気情報をセット
            var mainColumnSet = new ColumnSet();
            card.Body.Add(mainColumnSet);

            var mainColumn = new Column();
            mainColumnSet.Columns.Add(mainColumn);

            var mainText = new TextBlock()
            {
                Text = $"{model.publicTime.Date.ToString("M/d")}",
                Size = TextSize.Large,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            mainColumn.Items.Add(mainText);

            // 天気アイコンをセット
            var mainImage = new AdaptiveCards.Image();
            mainImage.Url = model.forecasts[0].image.url;
            mainImage.HorizontalAlignment = HorizontalAlignment.Center;
            mainColumn.Items.Add(mainImage);
        }

        private static void AddTextBlock(Column column, string text, TextSize size, HorizontalAlignment alignment)
        {
            column.Items.Add(new TextBlock()
            {
                Text = text,
                Size = size,
                HorizontalAlignment = alignment
            });
        }
        private static void AddImage(Column column, string url, ImageSize size, HorizontalAlignment alignment)
        {
            column.Items.Add(new AdaptiveCards.Image()
            {
                Url = url,
                Size = size,
                HorizontalAlignment = alignment
            });
        }

        private static void AddWeather(WeatherModel model, AdaptiveCard card)
        {
            // タイトル作成
            var titleColumnSet = new ColumnSet();
            card.Body.Add(titleColumnSet);

            var titleColumn = new Column();
            titleColumnSet.Columns.Add(titleColumn);
            AddTextBlock(titleColumn, $"{model.location.city} の天気", TextSize.ExtraLarge, HorizontalAlignment.Center);

            // 本文作成
            // 天気情報をセット
            var mainColumnSet = new ColumnSet();
            card.Body.Add(mainColumnSet);

            foreach (var item in model.forecasts)
            {
                var mainColumn = new Column();
                mainColumnSet.Columns.Add(mainColumn);

                // 天気データの取得と加工
                string day = item.dateLabel;
                string date = DateTime.Parse(item.date).Date.ToString("M/d");

                // temperature が null の場合は "--" に変換
                string maxTemp, minTemp;
                try
                {
                    maxTemp = item.temperature.max.celsius;
                    minTemp = item.temperature.min.celsius;
                }
                catch
                {
                    maxTemp = "--";
                    minTemp = "--";
                }

                // データのセット
                AddTextBlock(mainColumn, $"{day}({ date})", TextSize.Large, HorizontalAlignment.Center);
                AddTextBlock(mainColumn, $"{maxTemp} / {minTemp} °C", TextSize.Medium, HorizontalAlignment.Center);
                AddImage(mainColumn, item.image.url, ImageSize.Medium, HorizontalAlignment.Center);
            }

        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataApiService.Models;
using DataApiService.Utils;

namespace DataApiService
{
    /// <summary>
    /// Интерфейс менеджера работы с WebApi
    /// </summary>
    public interface IDataManager
    {
        /// <summary>
        /// Запрос списка событий
        /// </summary>
        /// <typeparam name="T">Возвращаемый тип</typeparam>
        /// <param name="pointName">Название точки доступа</param>
        /// <param name="getParams">Набор параметров Get запроса</param>
        /// <returns>Набор значений событий API</returns>
        Task<IEnumerable<T>> GetItems<T>(string pointName, Dictionary<string, string> getParams = null);

        /// <summary>
        /// Аутентификация на сервисе
        /// </summary>
        /// <param name="login">Логин</param>
        /// <param name="password">Пароль</param>
        /// <returns>Устанавливает токен идентификации, или бросает ошибку в случае отказа сервера</returns>
        void Auth(string login, string password);
        public bool SendCustomRequest(string pointName, Dictionary<string, string> paramDict = null, object machine = null, string method = "POST");
    }

    /// <summary>
    /// Настройки подключения
    /// </summary>
    public class BaseApiOptions
    {
        /// <summary>
        /// Токент сервиса
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Адрес веб-сервера (полный URL)
        /// </summary>
        /// <remarks>
        /// Свойство обределяется при старте сервера, значение берется из файла настроек
        /// appsettings.json
        /// ключ ApiWebAddress
        /// по умолчанию равен http://178.57.220.37:98, если в конфигурации нет настройки используется значение по умолчанию
        /// </remarks>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Адрес точки с токеном
        /// </summary>
        /// <param name="controllerName">Контроллер точки</param>
        /// <returns>Строка URL</returns>
        public string GetUrlApiService(string controllerName)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new ArgumentNullException("Token не получен");
            }

            return $"{BaseUrl}/{controllerName}?token={Token}";
        }



    }


    /// <summary>
    /// Объект ответа на запрос токена
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Строка токена
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; }

        /// <summary>
        /// Идентификатор ??
        /// </summary>
        [JsonPropertyName("owner_id")]
        public int Owner_id { get; set; }

        /// <summary>
        /// Строка для случая ошибочной аутентификации
        /// </summary>
        [JsonPropertyName("result")]
        public string Result { get; set; }
    }


    /// <summary>
    /// Менеджер работы с WebApi
    /// </summary>
    public class DataManager : IDataManager
    {
        private BaseApiOptions _options;
        private WebClient _client;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="options">Настройки подключения</param>
        public DataManager(BaseApiOptions options)
        {
            _options = options;

            //Класс веб клиента
            _client = new WebClient();
            _client.Headers.Add(HttpRequestHeader.Accept, "application/json");
        }

        /// <summary>
        /// Аутентификация на сервисе
        /// </summary>
        /// <param name="login">Логин</param>
        /// <param name="password">Пароль</param>
        /// <returns>Устанавливает токен идентификации, или бросает ошибку в случае отказа сервера</returns>
        public void Auth(string login, string password)
        {
            _options.Token = GetServiceToken(_options.BaseUrl, login, password).Result;
        }

        /// <summary>
        /// Аутентификация на сервисе
        /// TODO по идее под аутентификацию нужен свой класс или сервис
        /// </summary>
        /// <param name="login">Имя пользователя</param>
        /// <param name="password">Пароль</param>
        /// <returns>Токен доступа к сервису</returns>
        private async Task<string> GetServiceToken(string url, string login, string password)
        {
            var authUrl = $"{url}/token";
            var responce = await GetRequestAsync<TokenResponse>(authUrl, new Dictionary<string, string>()
            {
                {"login",login },
                {"password",password }
            });
            //Если result есть в ответе, значит ошибка аутентификации
            if (!string.IsNullOrEmpty(responce.Result))
            {
                throw new Exception(responce.Result);
            }
            //Полученный с сервера токен
            return responce.Token;
        }

        /// <summary>
        /// HTTP GET на сервер
        /// </summary>
        /// <typeparam name="T">Возвращаемый тип</typeparam>
        /// <param name="url">Строка URL сервиса</param>
        /// <param name="pars">Набор параметров</param>
        /// <returns>Полученный ответ с сервера десериализуется из JSON в тип T</returns>
        private async Task<T> GetRequestAsync<T>(string url, Dictionary<string, string> pars)
        {
            var paramString = pars.ToGetParameters();
            var destUrl = $"{url}?{paramString}";
            var responseData = await _client.DownloadDataTaskAsync(new Uri(destUrl));
            var result = JsonSerializer.Deserialize<T>(System.Text.Encoding.UTF8.GetString(responseData));
            return result;
        }

        /// <summary>
        /// Запрос списка элементов
        /// </summary>
        /// <remarks>
        /// Ошибки пробрасываются наверх, вызывающему
        /// </remarks>
        /// <typeparam name="T">Возвращаемый тип</typeparam>
        /// <param name="pointName">Название точки доступа</param>
        /// <param name="getParams">Набор параметров Get запроса</param>
        /// <returns>Набор значений событий API</returns>
        public async Task<IEnumerable<T>> GetItems<T>(string pointName, Dictionary<string, string> getParams = null)
        {
            try
            {
                //Адрес сервиса с токеном
                string urlService = _options.GetUrlApiService(pointName);
                var paramString = getParams.ToGetParameters();

                var url = new Uri($"{urlService}{paramString}");

                var responseData = await _client.DownloadDataTaskAsync(url);
                var jsonStr = System.Text.Encoding.UTF8.GetString(responseData);
                var result = JsonSerializer.Deserialize<IEnumerable<T>>(jsonStr);
                return result;
            }
            catch (Exception)
            {

                throw;
            }

        }

        public bool SendCustomRequest(string pointName, Dictionary<string, string> paramDict = null, object machine = null, string method = "POST")
        {
            string urlService = _options.GetUrlApiService(pointName);
            var paramString = (paramDict ?? new Dictionary<string, string>()).ToGetParameters();
            var url = new Uri($"{urlService}{paramString}");
            var jsonObj = JsonSerializer.Serialize(machine);
            if(machine != null)
            {
                _client.Headers[HttpRequestHeader.ContentType] = "application/json-patch+json";
            }
            var resp = _client.UploadString(url, method, jsonObj);
            return resp == "";
        }
    }
}

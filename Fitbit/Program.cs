
namespace Fitbit
{
    using Fitbit.Api;
    using Fitbit.Models;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml.Serialization;

    class Program
    {
        const string consumerKey = "";
        const string consumerSecret = "";

        static void Main(string[] args)
        {
            //Example of getting the Auth credentials for the first time by directoring the
            //user to the fitbit site to get a PIN. 

            var credentials = LoadCredentials();

            if (credentials == null)
            {
                credentials = Authenticate();
                SaveCredentials(credentials);
            }

            var fitbit = new FitbitClient(consumerKey, consumerSecret, credentials.AuthToken, credentials.AuthTokenSecret);

            var profile = fitbit.GetUserProfile();
            Console.WriteLine(" Your profile name            : {0}", profile.DisplayName);

            // Sample to show how to retrieve data for a time range. 
            // Assumes a 'Challenge' of a million steps required in a little more 260 days.

            DateTime startDate = new DateTime(2014, 11, 11);
            DateTime endDate = new DateTime(2015, 07, 31);
            double challengeSteps = 1000000;

            double numDays = (DateTime.UtcNow - startDate).TotalDays;
            double numChallengeDays = (endDate - startDate).TotalDays;
            var timeSeriesData = fitbit.GetTimeSeries(TimeSeriesResourceType.Steps, startDate, endDate);
            int totalSteps = 0;
            foreach (var dataItem in timeSeriesData.DataList)
            {
                totalSteps += Int32.Parse(dataItem.Value);
            }

            Console.WriteLine(" Total steps since challenge  : {0:n0}", totalSteps);
            Console.WriteLine(" Percentage time completed    : {0:n2}/{1} = {2:n2}%", numDays, numChallengeDays, numDays / numChallengeDays * 100);
            Console.WriteLine(" Percentage towards goal      : {0:n2}%", totalSteps / challengeSteps * 100.00);
            Console.WriteLine(" Initial required steps/day   : {0:n0}", challengeSteps / numChallengeDays);
            Console.WriteLine(" Current steps/day            : {0:n0}", totalSteps / numDays);
            Console.WriteLine(" Remaining required steps/day : {0:n0}", (challengeSteps - totalSteps) / (numChallengeDays - numDays));

            Console.ReadLine();
        }

        static AuthCredential Authenticate()
        {
            var requestTokenUrl = "http://api.fitbit.com/oauth/request_token";
            var accessTokenUrl = "http://api.fitbit.com/oauth/access_token";
            var authorizeUrl = "http://www.fitbit.com/oauth/authorize";

            var a = new Authenticator(consumerKey, consumerSecret, requestTokenUrl, accessTokenUrl, authorizeUrl);

            RequestToken token = a.GetRequestToken();

            var url = a.GenerateAuthUrlFromRequestToken(token, false);

            Process.Start(url);

            Console.WriteLine("Enter the verification code from the website");
            var pin = Console.ReadLine();

            var credentials = a.GetAuthCredentialFromPin(pin, token);
            return credentials;
        }

        static void SaveCredentials(AuthCredential credentials)
        {
            try
            {
                var path = GetAppDataPath();
                var serializer = new XmlSerializer(typeof(AuthCredential));
                TextWriter writer = new StreamWriter(path);
                serializer.Serialize(writer, credentials);
                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static AuthCredential LoadCredentials()
        {
            AuthCredential credentials = null;
            try
            {
                var path = GetAppDataPath();

                if (File.Exists(path))
                {
                    var serializer = new XmlSerializer(typeof(AuthCredential));
                    FileStream fs = new FileStream(path, FileMode.Open);

                    credentials = serializer.Deserialize(fs) as AuthCredential;
                    fs.Close();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return credentials;
        }

        static string GetAppDataPath()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fitbit");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return Path.Combine(path, "Credentials.xml");
        }
    }
}

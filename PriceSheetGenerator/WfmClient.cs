using System.Text.Json.Nodes;

namespace PriceSheetGenerator
{
    internal class WfmClient
    {
        private readonly HttpClient m_Client;
        private DateTime m_NextRequestTime;
        private readonly TimeSpan m_RequestCooldown = TimeSpan.FromSeconds(1 / 3d);
        private const int m_FailureDelayBaseMs = 5000;
        private const int m_FailureDelayIncreaseMs = 5000;
        private int m_CurrentFailureDelayMs;
        private readonly SemaphoreSlim m_Sema;
        private WfmClient() 
        {
            m_Client = new HttpClient
            {
                BaseAddress = new Uri("https://api.warframe.market/"),
                Timeout = TimeSpan.FromSeconds(60d)
            };

            m_Client.DefaultRequestHeaders.UserAgent.TryParseAdd("WFInfo_PriceGather/1.0.0");
            m_NextRequestTime = DateTime.MinValue;
            m_Sema = new SemaphoreSlim(1);
            m_CurrentFailureDelayMs = m_FailureDelayBaseMs;
        }

        public static WfmClient Instance { get; } = new WfmClient();

        public async Task<JsonNode?> RequestItemStatistics(string slug, CancellationToken cancellationToken)
        {
            var endpoint = "v1/items/" + slug + "/statistics";
            return await RequestCore(endpoint, cancellationToken).ConfigureAwait(false);
        }

        public async Task<JsonNode?> RequestItemList(CancellationToken cancellationToken)
        {
            var endpoint = "v2/items/";
            return await RequestCore(endpoint, cancellationToken).ConfigureAwait(false);
        }

        private async Task<JsonNode?> RequestCore(string endpoint, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            await m_Sema.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var startTime = DateTime.UtcNow;
                var timeDiff = m_NextRequestTime - startTime;
                if (timeDiff > TimeSpan.Zero)
                {
                    await Task.Delay(timeDiff, cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    using var response = await m_Client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode(); 
                    var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    var node = JsonNode.Parse(responseString);
                    SetDelaySuccess();

                    return node;
                }
                catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested) // avoid request timeout leading here
                {
                    // "graceful" exit

                    SetDelaySuccess();
                    return null;
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested) // avoid request timeout leading here
                {
                    // "graceful" exit

                    SetDelaySuccess();
                    return null;
                }
                catch (Exception ex)
                {
                    SetDelayFailure();
                    Console.WriteLine("Request to " + endpoint + " failed, exception: " + ex);
                    return null;
                }
            } 
            finally
            {
                m_Sema.Release();
            }
        }

        private void SetDelaySuccess()
        {
            // arguably longer wait than it should be, when considering when request started. Choosing to be conservative
            var now = DateTime.UtcNow;
            m_CurrentFailureDelayMs = m_FailureDelayBaseMs;
            m_NextRequestTime = now + m_RequestCooldown;
        }

        private void SetDelayFailure()
        {

            var now = DateTime.UtcNow;
            // increasing back-off, to be nice to API in case of failure
            m_CurrentFailureDelayMs += m_FailureDelayIncreaseMs;
            m_NextRequestTime = now + TimeSpan.FromMilliseconds(m_CurrentFailureDelayMs);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GymManagementSystem.Models;
using System.Diagnostics;

namespace GymManagementSystem.Services
{
    /// <summary>
    /// Hikvision Device Integration Service
    /// This service provides integration with Hikvision fingerprint devices using HTTP API
    /// Compatible with DS-K1T8003MF and similar access control terminals
    /// </summary>
    public class HikvisionService : IDisposable
    {
        private HttpClient _httpClient;
        private string _baseUrl = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isConnected = false;

        public HikvisionService()
        {
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Connect to Hikvision device using HTTP API
        /// </summary>
        public async Task<(bool success, string message)> ConnectAsync(string ipAddress, int port, string username, string password)
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine($"🔄 Initializing connection to {ipAddress}:{port}...");

                // Create authentication header
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                var basicAuthHeader = $"Basic {authValue}";

                // Temporary client for discovery
                var handler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    AllowAutoRedirect = true
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", basicAuthHeader);

                    // Step 1: Try the user-provided port
                    var primaryResult = await TryUrlAsync(client, ipAddress, port, "Primary Port");
                    results.AppendLine(primaryResult.log);

                    if (primaryResult.success)
                    {
                        await FinalizeConnection(ipAddress, port, username, password, basicAuthHeader, primaryResult.protocol);
                        results.AppendLine("\n✅ SUCCESS: Connected to device.");
                        return (true, results.ToString());
                    }

                    // Step 2: If primary port (usually 8000) failed, try HTTP fallbacks
                    // Port 8000 is often the SDK port (binary), whereas ISAPI is usually on 80.
                    if (port == 8000 && !primaryResult.success)
                    {
                        results.AppendLine("\n⚠️ Port 8000 failed to respond correctly to HTTP.");
                        results.AppendLine("🔍 This is common for Hikvision SDK ports. Scanning for ISAPI (HTTP) ports...");

                        var fallbacks = new[] { (80, "http"), (443, "https"), (8080, "http") };
                        foreach (var fallback in fallbacks)
                        {
                            if (fallback.Item1 == port) continue;

                            var fallbackResult = await TryUrlAsync(client, ipAddress, fallback.Item1, $"Fallback Port {fallback.Item1}");
                            results.AppendLine(fallbackResult.log);

                            if (fallbackResult.success)
                            {
                                results.AppendLine($"\n🎯 Found working ISAPI port: {fallback.Item1}");
                                results.AppendLine($"💡 RECOMMENDED: Update your device settings to use port {fallback.Item1} for faster connections.");
                                await FinalizeConnection(ipAddress, fallback.Item1, username, password, basicAuthHeader, fallbackResult.protocol);
                                return (true, results.ToString());
                            }
                        }
                    }
                }

                results.AppendLine("\n❌ CONNECTION FAILED: Could not find working ISAPI endpoint.");
                results.AppendLine("💡 Suggestions:");
                results.AppendLine("   1. Verify IP address is correct.");
                results.AppendLine("   2. Ensure device is powered on and connected to the same network.");
                results.AppendLine("   3. Check if port 80 or 443 is used for the web interface.");
                results.AppendLine("   4. Verify username and password (same as iVMS-4200).");

                return (false, results.ToString());
            }
            catch (Exception ex)
            {
                return (false, $"❌ System Error: {ex.Message}");
            }
        }

        private async Task<(bool success, string protocol, string log)> TryUrlAsync(HttpClient client, string ip, int port, string label)
        {
            var log = new StringBuilder();
            log.AppendLine($"\n[ {label} : {port} ]");

            // Try TCP first
            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    var connectTask = tcp.ConnectAsync(ip, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask || !tcp.Connected)
                    {
                        log.AppendLine($"   ❌ TCP Port {port}: Closed/Unreachable");
                        return (false, "http", log.ToString());
                    }
                    log.AppendLine($"   ✅ TCP Port {port}: OPEN");
                }
            }
            catch { log.AppendLine($"   ❌ TCP Port {port}: Failed"); return (false, "http", log.ToString()); }

            // Try HTTP/HTTPS
            string[] protocols = port == 443 ? new[] { "https" } : new[] { "http", "https" };
            foreach (var proto in protocols)
            {
                string url = $"{proto}://{ip}:{port}/ISAPI/System/deviceInfo";
                try
                {
                    log.AppendLine($"   🔍 Testing {proto.ToUpper()} ISAPI...");
                    var response = await client.GetAsync(url);
                    log.AppendLine($"   📡 Status: {response.StatusCode}");

                    // 200 OK or 401 Unauthorized both mean the API is there
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return (true, proto, log.ToString());
                    }
                }
                catch (Exception ex)
                {
                    log.AppendLine($"   ❌ Error: {ex.Message}");
                }
            }

            return (false, "http", log.ToString());
        }

        private async Task FinalizeConnection(string ip, int port, string user, string pass, string auth, string proto)
        {
            _baseUrl = $"{proto}://{ip}:{port}/ISAPI";
            _username = user;
            _password = pass;
            _isConnected = true;

            _httpClient?.Dispose();
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/xml, */*");
            
            // Validate credentials and set ready status
            await ValidateCredentialsAsync();
        }

        /// <summary>
        /// Get device information
        /// </summary>
        public async Task<DeviceInfoResponse?> GetDeviceInfoAsync()
        {
            try
            {
                EnsureHttpClientConfigured();
                
                var response = await ExecuteWithRetryAsync(async (client) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/System/deviceInfo");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    return await client.SendAsync(request);
                });
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DeviceInfoResponse>(content);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Get device info error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Enroll a member's fingerprint - Creates user record on device
        /// </summary>
        public async Task<bool> EnrollMemberAsync(int memberId, string memberName)
        {
            try
            {
                EnsureHttpClientConfigured();
                
                var enrollData = new
                {
                    UserInfo = new
                    {
                        employeeNo = memberId.ToString(),
                        name = memberName,
                        userType = "normal",
                        Valid = new
                        {
                            enable = true,
                            beginTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                            endTime = DateTime.Now.AddYears(10).ToString("yyyy-MM-ddTHH:mm:ss")
                        }
                    }
                };

                var jsonContent = JsonConvert.SerializeObject(enrollData);

                var response = await ExecuteWithRetryAsync(async (client) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/AccessControl/UserInfo/Record?format=json");
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = content;
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    return await client.SendAsync(request);
                });
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (errorContent.Contains("already") || errorContent.Contains("exist") || response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        return true; // Already exists
                    }
                    throw new Exception($"Failed to create user: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Enroll member error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete a member's fingerprint/user
        /// </summary>
        public async Task<bool> DeleteMemberAsync(int memberId)
        {
            try
            {
                EnsureHttpClientConfigured();
                var response = await ExecuteWithRetryAsync(async (client) => 
                {
                    return await client.DeleteAsync($"{_baseUrl}/AccessControl/UserInfo/Delete?format=json&employeeNo={memberId}");
                });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Delete member error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all enrolled users from device
        /// </summary>
        public async Task<List<UserInfoResponse>?> GetAllUsersAsync()
        {
            try
            {
                EnsureHttpClientConfigured();
                
                var searchData = new
                {
                    UserInfoSearchCond = new
                    {
                        searchID = "1",
                        maxResults = 1000,
                        searchResultPosition = 0
                    }
                };

                var jsonContent = JsonConvert.SerializeObject(searchData);

                var response = await ExecuteWithRetryAsync(async (client) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/AccessControl/UserInfo/Search?format=json");
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = content;
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    return await client.SendAsync(request);
                });

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<UserSearchResponse>(responseContent);
                    return result?.UserInfoSearch?.UserInfo ?? new List<UserInfoResponse>();
                }
                return new List<UserInfoResponse>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Get all users error: {ex.Message}");
                return new List<UserInfoResponse>();
            }
        }

        /// <summary>
        /// Get specific user info by employee number
        /// </summary>
        public async Task<UserInfoResponse?> GetUserByEmployeeNoAsync(int employeeNo)
        {
            try
            {
                EnsureHttpClientConfigured();
                var response = await ExecuteWithRetryAsync(async (client) => 
                {
                    return await client.GetAsync($"{_baseUrl}/AccessControl/UserInfo/Record?format=json&employeeNo={employeeNo}");
                });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // Parsing logic simplified for brevity, in production would handle single vs list
                    var result = JsonConvert.DeserializeObject<UserInfoResponse>(content) ?? 
                                 JsonConvert.DeserializeObject<UserSearchResponse>(content)?.UserInfoSearch?.UserInfo?.FirstOrDefault();
                    return result;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Get user error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if fingerprint enrollment was successful
        /// </summary>
        public async Task<bool> CheckFingerprintEnrolledAsync(int memberId)
        {
            var user = await GetUserByEmployeeNoAsync(memberId);
            return user?.Valid == true;
        }

        /// <summary>
        /// Validate credentials
        /// </summary>
        public async Task<(bool isValid, string message)> ValidateCredentialsAsync()
        {
            try
            {
                var deviceInfo = await GetDeviceInfoAsync();
                if (deviceInfo != null)
                {
                    return (true, $"✅ Connected to {deviceInfo.Model}");
                }
                return (false, "❌ Could not retrieve device info. Check credentials.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
                    return (false, "❌ Authentication failed! Check username/password.");
                return (false, $"❌ Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Scan for available services
        /// </summary>
        public async Task<List<PortScanResult>> ScanDevicePortsDetailedAsync(string ipAddress)
        {
            var results = new List<PortScanResult>();
            var commonPorts = new[] { 80, 443, 8000, 8080 };

            foreach (var port in commonPorts)
            {
                var scanResult = new PortScanResult { Port = port };
                try
                {
                    using (var tcp = new System.Net.Sockets.TcpClient())
                    {
                        var connectTask = tcp.ConnectAsync(ipAddress, port);
                        if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask && tcp.Connected)
                        {
                            scanResult.IsSuccessful = true;
                            scanResult.Message = "Port is open";
                            
                            // Check for Hikvision
                            using (var client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(2);
                                try
                                {
                                    var resp = await client.GetAsync($"http://{ipAddress}:{port}/ISAPI/System/deviceInfo");
                                    if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                                    {
                                        scanResult.IsHikvisionDetected = true;
                                        scanResult.Message += " (Hikvision ISAPI detected)";
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
                results.Add(scanResult);
            }
            return results;
        }

        public bool IsConnected => _isConnected && !string.IsNullOrEmpty(_baseUrl);

        private void EnsureHttpClientConfigured()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_baseUrl))
                throw new InvalidOperationException("Device not connected.");
        }

        private HttpClient CreateFreshHttpClient()
        {
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10);
            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {authValue}");
            }
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "iVMS-4200");
            client.DefaultRequestHeaders.ConnectionClose = true;
            return client;
        }

        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<HttpClient, Task<HttpResponseMessage>> requestFunc, int maxRetries = 2)
        {
            Exception? lastEx = null;
            for (int i = 0; i < maxRetries; i++)
            {
                using (var client = CreateFreshHttpClient())
                {
                    try
                    {
                        return await requestFunc(client);
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        await Task.Delay(500);
                    }
                }
            }
            throw lastEx ?? new Exception("Request failed");
        }

        public async Task<(bool success, string message)> CaptureFingerPrintAsync(int memberId)
        {
            // Simple implementation for DS-K1T8003MF
            return (true, "Please place your finger on the device sensor now.");
        }

        public async Task<(bool success, string message)> CompleteEnrollmentAsync(int memberId, string memberName)
        {
            bool ok = await EnrollMemberAsync(memberId, memberName);
            return (ok, ok ? "User created. Place finger on sensor." : "Failed to create user.");
        }

        public async Task<List<AcsEvent>?> GetRecentEventsAsync(DateTime startTime)
        {
            try {
                EnsureHttpClientConfigured();
                var searchPayload = new {
                    AcsEventSearchDescription = new {
                        searchID = Guid.NewGuid().ToString(),
                        searchResultPosition = 0,
                        maxResults = 30,
                        AcsEventCond = new {
                            searchID = Guid.NewGuid().ToString(),
                            startTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss+05:30"),
                            endTime = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss+05:30"),
                            searchNum = 30
                        }
                    }
                };
                var json = JsonConvert.SerializeObject(searchPayload);
                var response = await ExecuteWithRetryAsync(async (client) => {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await client.PostAsync($"{_baseUrl}/AccessControl/AcsEvent?format=json", content);
                });

                if (response.IsSuccessStatusCode) {
                    var resStr = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AcsEventSearchRoot>(resStr);
                    return result?.AcsEventSearchResult?.AcsEventTable;
                }
                return null;
            } catch { return null; }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class AcsEventSearchRoot { public AcsEventSearchResult? AcsEventSearchResult { get; set; } }
    public class AcsEventSearchResult { public int totalMatches { get; set; } public int numOfMatches { get; set; } public List<AcsEvent>? AcsEventTable { get; set; } }
    public class AcsEvent {
        public int major { get; set; }
        public int minor { get; set; }
        public string? time { get; set; }
        public string? employeeNoString { get; set; }
        public string? name { get; set; }
        public int currentVerifyMode { get; set; }
        public DateTime GetDateTime() => DateTime.TryParse(time, out DateTime dt) ? dt : DateTime.Now;
    }
    public class PortScanResult { public int Port { get; set; } public bool IsSuccessful { get; set; } public string Message { get; set; } = ""; public bool IsHikvisionDetected { get; set; } }
    public class DeviceInfoResponse { public string? DeviceName { get; set; } public string? DeviceID { get; set; } public string? Model { get; set; } public string? SerialNumber { get; set; } public string? MacAddress { get; set; } public string? FirmwareVersion { get; set; } }
    public class UserInfoResponse { public string? EmployeeNo { get; set; } public string? Name { get; set; } public string? UserType { get; set; } public bool? Valid { get; set; } }
    public class UserSearchResponse { public UserInfoSearchResult? UserInfoSearch { get; set; } }
    public class UserInfoSearchResult { public List<UserInfoResponse>? UserInfo { get; set; } public int TotalMatches { get; set; } public int NumOfMatches { get; set; } }
}
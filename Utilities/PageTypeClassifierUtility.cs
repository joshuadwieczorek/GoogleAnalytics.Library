using System.Collections.Generic;
using System.Linq;
using Database.Accounts.Domain;
using Database.Accounts.Domain.configurations;

namespace GoogleAnalytics.Library.Utilities
{
    public class PageTypeClassifierUtility
    {
        private readonly List<GoogleVdpUrlPattern> _vdpUrlPatterns;
        private readonly List<SrpPagePattern> _srpPagePatterns;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="googleAccount"></param>
        /// <param name="srpPagePatterns"></param>
        public PageTypeClassifierUtility(
              List<GoogleVdpUrlPattern> vdpUrlPatterns
            , List<SrpPagePattern> srpPagePatterns)
        {
            _vdpUrlPatterns = vdpUrlPatterns;
            _srpPagePatterns = srpPagePatterns;
        }


        /// <summary>
        /// Classify page type of url.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public Database.Accounts.Domain.WebPageType Classify(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                // Convert url to lower.
                url = url.ToLower();

                // Check if is Home page.
                if (url.ToLower().Trim() == "/")
                    return Database.Accounts.Domain.WebPageType.Home;

                // Check if is SRP page.
                if (_srpPagePatterns is not null && _srpPagePatterns.Any())
                    foreach (var srpPagePattern in _srpPagePatterns)
                        if (!string.IsNullOrEmpty(srpPagePattern.SrpUrlPattern)
                            && url.Contains(srpPagePattern.SrpUrlPattern.ToLower()))
                            if (srpPagePattern.SrpUrlType == SrpUrlType.New)
                                return Database.Accounts.Domain.WebPageType.NewSrp;
                            else
                                return Database.Accounts.Domain.WebPageType.UsedSrp;

                // Check if is VDP url.
                if (_vdpUrlPatterns is not null && _vdpUrlPatterns.Any())
                    foreach (var vdpUrls in _vdpUrlPatterns)
                    {
                        // Check if is new VDP url.
                        if (!string.IsNullOrEmpty(vdpUrls.NewVdpUrlPattern)
                            && url.Contains(vdpUrls.NewVdpUrlPattern.ToLower()))
                            return Database.Accounts.Domain.WebPageType.NewVdp;

                        // Check if is used VDP url.
                        if (!string.IsNullOrEmpty(vdpUrls.UsedVdpUrlPattern)
                            && url.Contains(vdpUrls.UsedVdpUrlPattern.ToLower()))
                            return Database.Accounts.Domain.WebPageType.UsedVdp;

                        // Check if is certified VDP url.
                        if (!string.IsNullOrEmpty(vdpUrls.CertifiedVdpUrlPattern)
                            && url.Contains(vdpUrls.CertifiedVdpUrlPattern.ToLower()))
                            return Database.Accounts.Domain.WebPageType.CertifiedVdp;
                    }
            }

            // Otherwise return Other page type.
            return Database.Accounts.Domain.WebPageType.Other;
        }
    }
}
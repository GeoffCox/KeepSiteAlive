KeepSiteAlive
=============

BellaCodeKeepSiteAlive is a Windows Service that pings URLS at regular intervals.  

This is most often used to prevent application pools from shutting down for sites that don't have heavy internet traffic.

## Installation
1. Build and/or copy the BellaCodeKeepSiteAlive to your computer.  
   * I recommend Program Files\BellaCodeKeepSiteAlive as a good location.
2. Create/Modify the SiteUrls.txt file in the same directory as the EXE.
   * Each line of the file represents a web site to ping regularly.
   * Each line should be the time between pings (hh:mm:ss), a space, and then the URL.
   * This line will ping the BellaCode site every 1 minute and 3 seconds
  
    `00:01:03 http://www.bellacode.com/`

3. Open a command prompt with administrator permission and run the EXE with the installation switch.

    `BellaCodeKeepSiteAlive /i`

4. Start the service.

    `net start BellaCodeKeepSiteAlive`
    
    
## Diagnostics
You can look in the Application event log for the BellaCodeKeepSiteAlive event source.  It will log the following:
* Service Starting/Started and the number of sites it found.
* If there is an exception loading SiteUrls.txt
* When each site's keep alive thread starts.
* If the site returns something other than OK (reported as a warning).
* If there is an exception calling a site (reported as an error)
* Service Stopping/Stopped.

## Not Implemented
These features are not implemented. Pull requests welcome.
* Sites requiring authentication
* Following redirects



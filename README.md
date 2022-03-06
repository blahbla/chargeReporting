# dependencies 
probably only useful for norway,
mysql db,
tibber subscription,
zaptec car chargers,

# about
For norwegian zaptec pro car charger installation on a common grid. 
Its a console app that sends emails based on price matching per kwh used in chargers per hour.
Uses tibber api to get hourly prices, needs a mysql db and to be scheduled to run once a day. 

# zaptec api
To find the email mappings you will have to look in the zaptec api 
https://api.zaptec.com/api/chargers
Data - Name

# linux systemd installation 
##send bills and summary reports
### nano /etc/systemd/system/chargeReportingBills.timer
[Unit]

Description=Run chargeReporting monthly to calculate prices and send bills


[Timer]

OnCalendar=&ast; &ast;-&ast;-01 1:00:00

Persistant=true


[Install]

WantedBy=timers.target

### nano /etc/systemd/system/chargeReportingBills.service
[Unit]

Description=chargeReporting calculate prices and send bills

[Service]

User=root

WorkingDirectory=/usr/local/bin

ExecStart=/usr/local/bin/chargeReporting -d "server=***;port=3306;user=***;password=***;database=***;SSL Mode=None" --emails "P1 - John Doe->xyz99@something.com","summary->admin@something.com" -f "sender@somewhere.com" -s "somesmtp.com" -u zaptecapiuser -p zaptecapipassword


[Install]

WantedBy=multi-user.target


## daily price fetching
### nano /etc/systemd/system/chargeReportingPriceFetch.timer
[Unit]

Description=Run chargeReporting dayly


[Timer]

OnCalendar=&ast;-&ast;-&ast; 5:00:00

Persistant=true


[Install]

WantedBy=timers.target


### nano /etc/systemd/system/chargeReportingPriceFetch.service
[Unit]

Description=chargeReporting save tibber prices to db


[Service]

User=root

WorkingDirectory=/usr/local/bin

ExecStart=/usr/local/bin/chargeReporting -p -d "server=***;port=3306;user=***;password=***;database=***;SSL Mode=None" -t ***


[Install]

WantedBy=multi-user.target


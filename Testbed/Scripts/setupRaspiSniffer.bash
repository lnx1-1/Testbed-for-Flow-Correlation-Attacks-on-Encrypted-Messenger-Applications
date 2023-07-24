#!/bin/bash
# Summary: This script installs packages to serve an Accesspoint and support a additional eth interface for using as a Sniffing network.
# The traffic from these interfaces is masqueraded and forwarded over eth0
# It allows for Capturing Traffic on both interfaces (br0) or on each one separately (eth1) (wlan0) with TCPdump.
# author: linus kurz
# email: linus.kurz@haw-hamburg.de
# ---------------------------------
# ----------  Variables -----------
country_code='DE'
wifiName='TestbedSniffNet'
wifiPW='DemoNewnet'
captureUserName='captureUsr'
# ---------------------------------
if [ $# -eq 0 ] || [ $# -gt 2 ]; then
	echo "Usage: ./script.sh <pw_of_capture_user> [-N]"
	echo
	echo "[-N] 		Non-Interactive. Does't ask for Overwrite"
	exit 2
fi

captureUserPW=$1

# ---------------------------------
retVal=$(id -u)
if [[ retVal -ne 0 ]]; then
        echo "Please run as root or with sudo"
        exit
fi
#----------------------------------

if [[ $2 -ne "-N" ]]; then
	# Warning! This script overwrites existing Network configurations
	echo "Warning! This script overwrites existing Network configurations"
	read -p "Do you want to proceed? (y/n) " yn
	case $yn in 
		y ) echo ok, we will proceed;;
		n ) echo exiting...;
			exit;;
		* ) echo invalid response;
			exit 1;;
	esac
else
	echo "Using non-interaktive Mode"
fi
echo "---------------------------------------------"
echo "			- Starting Device Setup - "
echo "--------------------------------------------"

#read -p "Test" yn

#------------------------------------
echo "		- Updating System -"
#------------------------------------
sudo apt-get update

sudo DEBIAN_FRONTEND=noninteractive apt-get -yq --allow-downgrades --allow-remove-essential --allow-change-held-packages upgrade



#------------------------------------
echo "		- Install Tools -"
#------------------------------------
sudo DEBIAN_FRONTEND=noninteractive apt-get -yq install bridge-utils hostapd tcpdump


#------------------------------------
echo "		- Disable wpa_supplicant - "
#------------------------------------
sudo systemctl disable wpa_supplicant.service
sudo systemctl disable wpa_supplicant@wlan0.service


#------------------------------------
echo "		- Switching to Networkmanager - "
#------------------------------------
sudo systemctl disable dhcpcd
sudo systemctl enable NetworkManager

#-----------------------------------
echo "		- Set Country Code -"
#-----------------------------------
sudo raspi-config nonint do_wifi_country $country_code

#------------------------------------------------
echo "		- unblock wifi with rfkill - "
#------------------------------------------------
sudo rfkill unblock all

#----------------------------------
echo "		- Setup NetworkManager Config -"
#-----------------------------------
echo "
[main]
plugins=ifupdown,keyfile

[ifupdown]
managed=false

[device]
wifi.scan-rand-mac-address=no
" > /etc/NetworkManager/NetworkManager.conf

echo "
[keyfile]
unmanaged-devices=interface-name:wlan0
" > /etc/NetworkManager/conf.d/unmanaged.conf

#------------------------------------
echo "		- Setup hostapd config -"
#---------------------------------
echo "# driver setup
bridge=br0
interface=wlan0
driver=nl80211

# country setup
country_code=$country_code

# a means 5 GHz
hw_mode=a
wmm_enabled=1

# auto select channel with least interference
# is channel=0 working?
channel=40
ieee80211ac=1
ieee80211n=1
ieee80211d=0
ieee80211h=0

# 802.11ac support

# basic setup
ssid=$wifiName
wpa=2
auth_algs=1
wpa_key_mgmt=WPA-PSK
wpa_passphrase=$wifiPW

# use AES instead of TKIP
rsn_pairwise=CCMP

require_ht=1" > /etc/hostapd/sniffNet.conf

#------------------------------------------------
echo "		- setup hostapd@sniffNet.service -" 
#------------------------------------------------
sudo systemctl unmask hostapd@sniffNet
sudo systemctl enable hostapd@sniffNet

#------------------------------------------------
echo "		- Setup and configure Systemd bridge- "
#------------------------------------------------
echo "
[NetDev]
Name=br0
Kind=bridge
" > /etc/systemd/network/br0.netdev


echo "[Match]
Name=br0

[Network]
Address=192.168.4.1/24
DHCPServer=true
IPForward=true
IPMasquerade=true

[DHCPServer]
PoolOffset=100
PoolSize=64
EmitDNS=yes
DNS=1.1.1.1
" > /etc/systemd/network/br0.network

echo "[Match]
Name=eth1

[Network]
Bridge=br0
" > /etc/systemd/network/bind_eth.network

#------------------------------------------------
echo "		- Enable Systemd - "
#------------------------------------------------
sudo systemctl enable systemd-networkd

#------------------------------------------------
echo "		- Adding Capture User -"
#------------------------------------------------
encryptedCaptPW=$(openssl passwd -crypt $captureUserPW)
useradd -m -p $encryptedCaptPW $captureUserName

#------------------------------------------------
echo "		- Setting up capture folder -"
#------------------------------------------------
sudo mkdir /home/$captureUserName/capture
sudo chown $captureUserName /home/$captureUserName/capture

#------------------------------------------------
echo "		- Configure tcpdump nonroot - "
#------------------------------------------------
sudo groupadd pcap
sudo usermod -a -G pcap $captureUserName

sudo chgrp pcap /usr/bin/tcpdump
sudo chmod 750 /usr/bin/tcpdump

sudo setcap cap_net_raw,cap_net_admin=eip /usr/bin/tcpdump

#------------------------------------------------
echo "------------------------------------------"
echo "		        Setup done	              	"
echo "------------------------------------------"
echo "		After reboot you can connect to Wifi: "$wifiName
echo "		With password: "$wifiPW
echo ""
echo "		Capture User: ["$captureUserName "]"
echo "		password: ["$captureUserPW"]"
echo " "
echo "		- Rebooting System  now - "
#------------------------------------------------
sudo systemctl reboot
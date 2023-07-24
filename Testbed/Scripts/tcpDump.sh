#!/bin/sh
sudo tcpdump -i wlan0 -w /home/lnx/captures/dump-%m%d_%H:%M:%S.pcap net 91.105.192.0/23 or net 91.108.4.0/22 or net 91.108.8.0/22 or net 91.108.12.0/22 or net 91.108.16.0/22 or net 91.108.20.0/22 or net 91.108.56.0/22 or net 149.154.160.0/20 or net 185.76.151.0/24

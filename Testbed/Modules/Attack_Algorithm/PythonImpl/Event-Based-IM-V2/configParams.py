TransitionPeriod = 4
amazon_ips = {} # Amazon meint wohl ob eingehend oder ausgehend
GAMMA = 0.25
observation_lengths = [180, 300, 900, 1800, 3600] # in seconds (3min - 30min)
LENGTH = 3600 #3600 #
"""Eq 3600"""
MAGIC_IsUserPacketARelevantBurstPacket = 40 # Wird bei der Event Extraction verwendet... Wenn größer, dann wird Paket als Relevantes Burst Paket verwendet.
DELTA = 15 # seconds -> Maximale Zeitliche Differenz.. Wird schrittweise erhöht 
size_threshold = 10000
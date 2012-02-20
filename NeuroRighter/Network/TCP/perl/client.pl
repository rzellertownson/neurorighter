#!/usr/bin/perl
#tcpclient.pl

use IO::Socket::INET;

# flush after every write
$| = 1;

my ($socket,$client_socket);

# creating object interface of IO::Socket::INET modules which internally creates
# socket, binds and connects to the TCP server running on the specific port.
$socket = new IO::Socket::INET (
  PeerHost => '128.61.139.90',
  PeerPort => '4545',
  Proto => 'tcp',
) or die "ERROR in Socket Creation : $!\n";

print "[TCP Connection Success.]\n";

# read the socket data sent by server.
#$data = <$socket>;
#print "Received from Server : $data\n";

# write on the socket to server.
$data = "0 digi 1 0 digi 0 400\n";
print $socket "$data";

$data = "1 digi 1 0 digi 0 400\n";
print $socket "$data";

$data = "1 digi 1 0 digi 0 400\n";
print $socket "$data";

$data = "1 digi 1 0 digi 0 400\n";
print $socket "$data";

# Close the socket
$socket->close();

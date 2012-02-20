#!/usr/bin/perl
# racs_serv.pl - simple server for TCP interface.
# Jon Newman, Feb. 2012.
#
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

use strict;
use IO::Socket::INET;
require "racsstep_req.pl";

# flush after every write
$| = 1;

my ($socket,$client_socket);

# creating object interface of IO::Socket::INET modules which internally does
# socket creation, binding and listening at the specified port address.
$socket = new IO::Socket::INET (
    LocalHost => '128.61.139.90',
    LocalPort => '4545',
    Proto => 'tcp',
    Listen => 1,
    Reuse => 1
) or die "ERROR in Socket Creation : $!\n";

# Inform user that we are waiting for clients
print STDERR "[SERVER accepting client input on port 4545...]\n";

# Wait for commands
while(1) {
    # waiting for new client connection.
    $client_socket = $socket->accept();

    # get the host and port number of newly connected client.
    print "[SERVER accepted New Client Connection]\n";

    # read operation on the newly accepted client
    while(<$client_socket>)
    {
    print STDERR "Sending $_ to racsstep.\n\n";
        step($_);
    }
}


sub INT_handler {

    # close the socket
    $socket->close();
    print STDERR "[SERVER accepting client input on port 4545 has been terminated...]\n";

    # exit politely
    exit(1);
}

# Assign interupt handler
$SIG{'INT'} = 'INT_handler';


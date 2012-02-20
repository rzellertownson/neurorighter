#!/usr/bin/perl -w
# racsstep_req.pl - modification of original racsstep string parser that provides functionality
# as a sub routine that can be used like a library in the racs_serv.pl server. This way, the strings
# accepted over TCP can be sent to the step sub routine, parsed and sent to the racs hardware.
#
# Copyright (C) 2004  Daniel Wagenaar (wagenaar@caltech.edu)
# Modified 2012 Jon Newman
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

sub usage {
  print STDERR <<"EOF";
Usage: racsstep [input file]

Input file format is: a series of lines of the following format:

  Time StepSpec [StepSpec ...]

where Time is time in ms since start of previous line,

      StepSpec is:

        Channel vlt delay

where CHANNEL is one of "dac", "sw", "aux" or "digi".
      VLT in digital steps,
      DELAY in us.

If CHANNEL is: then VLT is interpreted as:

  aux    signed digital value (-127..+127)
  sw     eCR, iCR channel number, or 0 for off
  dac    voltage
  digi   digital value

There can be any number of StepSpecs on a line.

EOF
  exit(1);
}

sub step{
#if (scalar(@ARGV) && $ARGV[0] =~ /^-/) {
  #usage();
#}

# ----------------------------------------------------------------------
my %cr2blk = (
  "e36" => "1A1",
  "i66" => "1A1",
  "e28" => "1A2",
  "i78" => "1A2",
  "e37" => "1A3",
  "i67" => "1A3",
  "e38" => "1A4",
  "i68" => "1A4",
  "e45" => "1A5",
  "i55" => "1A5",
  "e46" => "1A6",
  "i56" => "1A6",
  "e48" => "1A7",
  "i58" => "1A7",
  "e47" => "1A8",
  "i57" => "1A8",
  "e57" => "1B1",
  "i47" => "1B1",
  "e58" => "1B2",
  "i48" => "1B2",
  "e56" => "1B3",
  "i46" => "1B3",
  "e55" => "1B4",
  "i45" => "1B4",
  "e68" => "1B5",
  "i38" => "1B5",
  "e67" => "1B6",
  "i37" => "1B6",
  "e78" => "1B7",
  "i28" => "1B7",
  "e66" => "1B8",
  "i36" => "1B8",
  "e77" => "2A2",
  "i72" => "2A2",
  "e87" => "2A3",
  "i82" => "2A3",
  "e76" => "2A4",
  "i73" => "2A4",
  "e86" => "2A5",
  "i83" => "2A5",
  "e65" => "2A6",
  "i64" => "2A6",
  "e75" => "2A7",
  "i74" => "2A7",
  "e85" => "2A8",
  "i84" => "2A8",
  "e84" => "2B1",
  "i85" => "2B1",
  "e74" => "2B2",
  "i75" => "2B2",
  "e64" => "2B3",
  "i65" => "2B3",
  "e83" => "2B4",
  "i86" => "2B4",
  "e73" => "2B5",
  "i76" => "2B5",
  "e82" => "2B6",
  "i87" => "2B6",
  "e72" => "2B7",
  "i77" => "2B7",
  "e63" => "3A1",
  "i33" => "3A1",
  "e71" => "3A2",
  "i21" => "3A2",
  "e62" => "3A3",
  "i32" => "3A3",
  "e61" => "3A4",
  "i31" => "3A4",
  "e54" => "3A5",
  "i44" => "3A5",
  "e53" => "3A6",
  "i43" => "3A6",
  "e51" => "3A7",
  "i41" => "3A7",
  "e52" => "3A8",
  "i42" => "3A8",
  "e42" => "3B1",
  "i52" => "3B1",
  "e41" => "3B2",
  "i51" => "3B2",
  "e43" => "3B3",
  "i53" => "3B3",
  "e44" => "3B4",
  "i54" => "3B4",
  "e31" => "3B5",
  "i61" => "3B5",
  "e32" => "3B6",
  "i62" => "3B6",
  "e21" => "3B7",
  "i71" => "3B7",
  "e33" => "3B8",
  "i63" => "3B8",
  "e22" => "4A2",
  "i27" => "4A2",
  "e12" => "4A3",
  "i17" => "4A3",
  "e23" => "4A4",
  "i26" => "4A4",
  "e13" => "4A5",
  "i16" => "4A5",
  "e34" => "4A6",
  "i35" => "4A6",
  "e24" => "4A7",
  "i25" => "4A7",
  "e14" => "4A8",
  "i15" => "4A8",
  "e15" => "4B1",
  "i14" => "4B1",
  "e25" => "4B2",
  "i24" => "4B2",
  "e35" => "4B3",
  "i34" => "4B3",
  "e16" => "4B4",
  "i13" => "4B4",
  "e26" => "4B5",
  "i23" => "4B5",
  "e17" => "4B6",
  "i12" => "4B6",
  "e27" => "4B7",
  "i22" => "4B7",
  );
my %blk2val = (
  "1A1" => 0,
  "1A2" => 2,
  "1A3" => 128,
  "1A4" => 130,
  "1A5" => 1,
  "1A6" => 3,
  "1A7" => 129,
  "1A8" => 131,
  "1B1" => 16,
  "1B2" => 144,
  "1B3" => 17,
  "1B4" => 145,
  "1B5" => 18,
  "1B6" => 146,
  "1B7" => 19,
  "1B8" => 147,
  "2A1" => 8,
  "2A2" => 10,
  "2A3" => 136,
  "2A4" => 138,
  "2A5" => 9,
  "2A6" => 11,
  "2A7" => 137,
  "2A8" => 139,
  "2B1" => 24,
  "2B2" => 152,
  "2B3" => 25,
  "2B4" => 153,
  "2B5" => 26,
  "2B6" => 154,
  "2B7" => 27,
  "2B8" => 155,
  "3A1" => 56,
  "3A2" => 58,
  "3A3" => 184,
  "3A4" => 186,
  "3A5" => 57,
  "3A6" => 59,
  "3A7" => 185,
  "3A8" => 187,
  "3B1" => 40,
  "3B2" => 168,
  "3B3" => 41,
  "3B4" => 169,
  "3B5" => 42,
  "3B6" => 170,
  "3B7" => 43,
  "3B8" => 171,
  "4A1" => 32,
  "4A2" => 34,
  "4A3" => 160,
  "4A4" => 162,
  "4A5" => 33,
  "4A6" => 35,
  "4A7" => 161,
  "4A8" => 163,
  "4B1" => 48,
  "4B2" => 176,
  "4B3" => 49,
  "4B4" => 177,
  "4B5" => 50,
  "4B6" => 178,
  "4B7" => 51,
  "4B8" => 179,
  );
my %cr2val = ();
for (keys %cr2blk) {
  $cr2val{$_[0]} = 64 | $blk2val{$cr2blk{$_[0]}};
}
for (keys %blk2val) {
  $cr2val{$_[0]} = 64 | $blk2val{$_[0]};
}
$cr2val{"0"} = 0;
$cr2val{"-"} = 0;
######################################################################

my %chmap = ( "dac" => 4, "sw" => 5, "aux" => 253, "digi" => 252, "rst" => 255 );
# keep in step with allchstim.h

sub inthdlr {
  print STDERR "racsstep: Terminating on SIGINT\n";
  $SIG{INT} = 'DEFAULT';
  print DEVICE pack("lscc",0,0,255,0); # reset device
  close DEVICE;
  exit(1);
}

open(DEVICE, ">/dev/rtf2") or die "Cannot write to /dev/rtf2: $!\n";
select DEVICE; $|=1; select STDOUT;
print DEVICE pack("lscc",0,0,255,0); # reset device

$SIG{INT}=\&inthdlr;

my $start = 0;
my $now = 0;
my $first = 1;

  chomp;
  #print STDERR "Read: $_[0]\n";
  if (/^\#/) {
    print STDERR "$_[0]\n";
    next;
  }
  
  my @args = split(/[ \t]+/,$_[0]);
  my $basedelay = 1000 * shift @args;
  $start += $basedelay;
  $start=$now=0 if $basedelay==0;

  my @cmds = ();
  while (@args) {
    my ($channel,$v1,$d) = splice(@args,0,3);
    if ($channel eq "aux") {
      $v1 = 128 - $v1;
    } elsif ($channel eq "dac") {
      $v1 = int(-127*$v1 + 128);
    } elsif ($channel eq "sw") {
      $v1 = $cr2val{$v1};
    }
    $channel = $chmap{$channel} unless $channel lt "a";
    #print STDERR "  $channel $v1 $d\n";
#    die "Bad command $channel:$v1:$d\n" if $v1<0 || $v1>255;
    push @cmds, [ $d, $channel, $v1 ];
  }

  # Create hardware signal
  my $command="";
  my $localtime;
  for (sort { $a->[0] <=> $b->[0] } @cmds) {
    my $req = $_->[0] + $start;
    $command .= pack("lscc",$req-$now,$_->[2],$_->[1],0);
    # print STDERR $req-$now, ":c", $_->[1], "=", $_->[2], " : ";
    $now = $req;
  }
  # print STDERR "\n";
  
  print DEVICE $command;
  $first = 0;

};
1;

# Should probably wait for device to become ready again...

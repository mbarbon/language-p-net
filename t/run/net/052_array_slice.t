#!/usr/bin/perl -w

print "1..4\n";

if( defined &Internals::Net::get_class ) {
    $int_class = Internals::Net::get_class( 'System.Int32' );
    $array_class = Internals::Net::get_class( 'System.Array' );

    $array = $array_class->CreateInstance( $int_class, 10 );
    for( my $i = 0; $i < 10; ++$i ) {
        $array->[$i] = $i;
    }

    @x = @{$array}[3, 5, 7];

    print $#x == 2 ? "ok\n" : "not ok - $#x\n";
    print $x[0] == 3 ? "ok\n" : "not ok - $x[0]\n";
    print $x[1] == 5 ? "ok\n" : "not ok - $x[1]\n";
    print $x[2] == 7 ? "ok\n" : "not ok - $x[2]\n";
} else {
    print "ok - skipped\n" for 1..4;
}

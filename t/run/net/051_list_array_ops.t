#!/usr/bin/perl -w

print "1..14\n";

if( defined &Internals::Net::get_class ) {
    $date_class = Internals::Net::get_class( 'System.DateTime' );

    $generic_list = Internals::Net::get_class( 'System.Collections.Generic.List`1' );
    $date_list = Internals::Net::specialize_type( $generic_list, $date_class );

    $array = Internals::Net::create( $date_list );
    $date1 = Internals::Net::create( $date_class, 2010, 12, 16, 23, 13, 42 );
    $date2 = Internals::Net::create( $date_class, 2010, 12, 16, 23, 13, 43 );

    push @$array, $date1, $date2;
    print @$array == 2 ? "ok\n" : "not ok\n";
    print $array->[0] eq $date1 ? "ok\n" : "not ok\n";
    print $array->[1] eq $date2 ? "ok\n" : "not ok\n";

    $val = pop @$array;
    print @$array == 1 ? "ok\n" : "not ok\n";
    print $array->[0] eq $date1 ? "ok\n" : "not ok\n";
    print $val eq $date2 ? "ok\n" : "not ok\n";

    unshift @$array, $date2, $date1;
    print @$array == 3 ? "ok\n" : "not ok\n";
    print $array->[0] eq $date2 ? "ok\n" : "not ok\n";
    print $array->[1] eq $date1 ? "ok\n" : "not ok\n";
    print $array->[2] eq $date1 ? "ok\n" : "not ok\n";

    $val = shift @$array;
    print @$array == 2 ? "ok\n" : "not ok\n";
    print $val eq $date2 ? "ok\n" : "not ok\n";
    print $array->[0] eq $date1 ? "ok\n" : "not ok\n";
    print $array->[1] eq $date1 ? "ok\n" : "not ok\n";
} else {
    print "ok - skipped\n" for 1..14;
}

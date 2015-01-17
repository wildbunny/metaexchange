-- phpMyAdmin SQL Dump
-- version 3.4.9
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Jan 17, 2015 at 06:44 PM
-- Server version: 5.1.61
-- PHP Version: 5.3.3

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `metaexchange`
--

-- --------------------------------------------------------

--
-- Table structure for table `exceptions`
--

CREATE TABLE IF NOT EXISTS `exceptions` (
  `uid` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `txid` varchar(64) NOT NULL,
  `message` varchar(255) NOT NULL,
  `date` datetime NOT NULL,
  `type` enum('bitcoinDeposit','bitsharesDeposit','bitcoinRefund','bitsharesRefund') NOT NULL,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=5 ;

-- --------------------------------------------------------

--
-- Table structure for table `ignored`
--

CREATE TABLE IF NOT EXISTS `ignored` (
  `uid` int(11) NOT NULL AUTO_INCREMENT,
  `txid` varchar(64) NOT NULL,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=13 ;

-- --------------------------------------------------------

--
-- Table structure for table `stats`
--

CREATE TABLE IF NOT EXISTS `stats` (
  `last_bitshares_block` int(10) unsigned NOT NULL DEFAULT '0',
  `last_bitcoin_block` varchar(64) DEFAULT NULL,
  `uid` int(11) NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=3 ;

--
-- Dumping data for table `stats`
--

INSERT INTO `stats` (`last_bitshares_block`, `last_bitcoin_block`, `uid`) VALUES
(0, NULL, 2);

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE IF NOT EXISTS `transactions` (
  `received_txid` varchar(64) NOT NULL,
  `sent_txid` varchar(64) NOT NULL,
  `asset` varchar(8) NOT NULL,
  `amount` decimal(18,8) NOT NULL,
  `date` datetime NOT NULL,
  `type` enum('bitcoinDeposit','bitsharesDeposit','bitcoinRefund','bitsharesRefund') NOT NULL,
  `notes` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`received_txid`,`sent_txid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
